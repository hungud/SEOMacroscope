﻿/*

	This file is part of SEOMacroscope.

	Copyright 2017 Jason Holland.

	The GitHub repository may be found at:

		https://github.com/nazuke/SEOMacroscope

	Foobar is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Foobar is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with Foobar.  If not, see <http://www.gnu.org/licenses/>.

*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Timers;
using System.Threading;

namespace SEOMacroscope
{

  public sealed class MacroscopeDocumentCollection : Macroscope
  {

    /**************************************************************************/

    private Dictionary<string,MacroscopeDocument> DocCollection;

    private MacroscopeJobMaster JobMaster;
    private MacroscopeNamedQueue NamedQueue;
    private MacroscopeSearchIndex SearchIndex;
    private MacroscopeDeepKeywordAnalysis AnalyzeKeywords;

    private Dictionary<string,MacroscopeHyperlinksIn> StructHyperlinksIn;
    private Dictionary<string,MacroscopeHyperlinksIn> StructlinksIn;

    private Dictionary<string,Boolean> StatsHistory;
    private Dictionary<string,int> StatsHostnames;
    private Dictionary<string,int> StatsTitles;
    private Dictionary<string,int> StatsDescriptions;
    private Dictionary<string,int> StatsKeywords;
    private Dictionary<string,int> StatsWarnings;
    private Dictionary<string,int> StatsErrors;
    private Dictionary<string,int> StatsChecksums;
    
    private Dictionary<string,decimal> StatsDurations;
    
    private List<Dictionary<string,int>> StatsDeepKeywordAnalysis;

    private int StatsUrlsInternal;
    private int StatsUrlsExternal;
    private int StatsUrlsSitemaps;

    private Semaphore SemaphoreRecalc;
    private System.Timers.Timer TimerRecalc;

    /**************************************************************************/

    public MacroscopeDocumentCollection ( MacroscopeJobMaster JobMaster )
    {

      this.SuppressDebugMsg = true;

      this.DebugMsg( "MacroscopeDocumentCollection: INITIALIZING..." );

      this.DocCollection = new Dictionary<string,MacroscopeDocument> ( 4096 );

      this.JobMaster = JobMaster;

      this.NamedQueue = new MacroscopeNamedQueue ();
      this.NamedQueue.CreateNamedQueue( MacroscopeConstants.RecalculateDocCollection );

      this.SearchIndex = new MacroscopeSearchIndex ();

      this.AnalyzeKeywords = new MacroscopeDeepKeywordAnalysis ();

      this.StructHyperlinksIn = new Dictionary<string,MacroscopeHyperlinksIn> ( 1024 );
      
      this.StatsHistory = new Dictionary<string,Boolean> ( 1024 );
      this.StatsHostnames = new Dictionary<string,int> ( 16 );
      this.StatsTitles = new Dictionary<string,int> ( 1024 );
      this.StatsDescriptions = new Dictionary<string,int> ( 1024 );
      this.StatsKeywords = new Dictionary<string,int> ( 1024 );
      this.StatsWarnings = new  Dictionary<string,int> ( 32 );
      this.StatsErrors = new  Dictionary<string,int> ( 32 );
      this.StatsChecksums = new  Dictionary<string,int> ( 1024 );

      this.StatsDurations = new Dictionary<string,decimal> ( 1024 );

      this.StatsDeepKeywordAnalysis = new  List<Dictionary<string,int>> ( 4 );
      for( int i = 0 ; i <= 3 ; i++ )
      {
        this.StatsDeepKeywordAnalysis.Add( new Dictionary<string,int> ( 1024 ) );
      }

      this.StatsUrlsInternal = 0;
      this.StatsUrlsExternal = 0;
      this.StatsUrlsSitemaps = 0;

      this.SemaphoreRecalc = new Semaphore ( 0, 1 );
      this.StartRecalcTimer();

      this.DebugMsg( "MacroscopeDocumentCollection: INITIALIZED." );

    }

    /**************************************************************************/

    ~MacroscopeDocumentCollection ()
    {
      this.DebugMsg( "MacroscopeDocumentCollection DESTRUCTOR CALLED" );
      this.StopRecalcTimer();
    }

    /** Document Collection Methods *******************************************/

    public Boolean ContainsDocument ( string Url )
    {
      Boolean sResult = false;
      if( this.DocCollection.ContainsKey( Url ) )
      {
        sResult = true;
      }
      return( sResult );
    }

    /** Create Document Methods *******************************************/
    
    public MacroscopeDocument CreateDocument ( string Url )
    {
      MacroscopeDocument msDoc = new MacroscopeDocument ( DocumentCollection: this, Url: Url );
      return( msDoc );
    }

    public MacroscopeDocument CreateDocument ( MacroscopeCredential Credential, string Url )
    {
      MacroscopeDocument msDoc = new MacroscopeDocument (
                                   DocumentCollection: this,
                                   Url: Url,
                                   Credential: Credential
                                 );
      return( msDoc );
    }

    /** Document Stats ********************************************************/

    public int CountDocuments ()
    {
      return( this.DocCollection.Count );
    }

    public int CountUrlsInternal ()
    {
      return( this.StatsUrlsInternal );
    }

    public int CountUrlsExternal ()
    {
      return( this.StatsUrlsExternal );
    }

    public int CountUrlsSitemaps ()
    {
      return( this.StatsUrlsSitemaps );
    }

    /**************************************************************************/

    public void AddDocument ( MacroscopeDocument msDoc )
    {
      this.AddDocument( msDoc.GetUrl(), msDoc );
    }

    // TODO: There may be a bug here, whereby two or more error pages are added multiple times.
    public void AddDocument ( string Url, MacroscopeDocument msDoc )
    {
      lock( this.DocCollection )
      {
        if( this.DocCollection.ContainsKey( Url ) )
        {
          this.DocCollection.Remove( Url );
          this.DocCollection.Add( Url, msDoc );
        }
        else
        {
          try
          {
            this.DocCollection.Add( Url, msDoc );
          }
          catch( ArgumentException ex )
          {
            this.DebugMsg( string.Format( "AddDocument: {0}", ex.Message ) );
          }
          catch( Exception ex )
          {
            this.DebugMsg( string.Format( "AddDocument: {0}", ex.Message ) );
          }
        }
      }
    }

    /**************************************************************************/

    public Boolean DocumentExists ( string Url )
    {
      Boolean bExists = false;
      if( this.DocCollection.ContainsKey( Url ) )
      {
        bExists = true;
      }
      return( bExists );
    }

    /**************************************************************************/

    public MacroscopeDocument GetDocument ( string Url )
    {
      MacroscopeDocument msDoc = null;
      if( ( Url != null ) && this.DocCollection.ContainsKey( Url ) )
      {
        msDoc = ( MacroscopeDocument )this.DocCollection[ Url ];
      }
      return( msDoc );
    }

    /**************************************************************************/

    public void RemoveDocument ( string Url )
    {
      if( this.DocCollection.ContainsKey( Url ) )
      {
        lock( this.DocCollection )
        {
          this.DocCollection.Remove( Url );
        }
      }
    }

    /**************************************************************************/

    public IEnumerable<MacroscopeDocument> IterateDocuments ()
    {
      lock( this.DocCollection )
      {
        foreach( string Url in this.DocumentKeys() )
        {
          yield return this.DocCollection[ Url ];
        }
      }
    }

    /**************************************************************************/

    public List<string> DocumentKeys ()
    {
      List<string> lKeys = new List<string> ();
      if( this.DocCollection.Count > 0 )
      {
        lock( this.DocCollection )
        {
          foreach( string Url in this.DocCollection.Keys )
          {
            lKeys.Add( Url );
          }
        }
      }
      return( lKeys );
    }

    /** HyperlinksIn **********************************************************/

    public MacroscopeHyperlinksIn GetDocumentHyperlinksIn ( string Url )
    {
    
      MacroscopeHyperlinksIn HyperlinksIn = null;
      
      lock( this.StructHyperlinksIn )
      {

        if( this.StructHyperlinksIn.ContainsKey( Url ) )
        {
          HyperlinksIn = this.StructHyperlinksIn[ Url ];
        }

      }
      
      return( HyperlinksIn );
      
    }

    public IEnumerable<string> IterateHyperlinksIn ()
    {
      lock( this.StructHyperlinksIn )
      {
        foreach( string Url in this.StructHyperlinksIn.Keys )
        {
          yield return Url;
        }
      }
    }

    /** Recalculate Stats Across DocCollection ********************************/

    private void StartRecalcTimer ()
    {
      this.DebugMsg( string.Format( "StartRecalcTimer: {0}", "STARTING..." ) );
      SemaphoreRecalc.Release( 1 );
      this.DebugMsg( string.Format( "StartRecalcTimer SemaphoreRecalc: {0}", "RELEASED" ) );
      this.TimerRecalc = new System.Timers.Timer ( 2000 );
      this.TimerRecalc.Elapsed += this.WorkerRecalculateDocCollection;
      this.TimerRecalc.AutoReset = true;
      this.TimerRecalc.Enabled = true;
      this.TimerRecalc.Start();
      this.DebugMsg( string.Format( "StartRecalcTimer: {0}", "STARTED." ) );
    }

    private void StopRecalcTimer ()
    {
      try
      {
        this.TimerRecalc.Stop();
        this.TimerRecalc.Dispose();
      }
      catch( Exception ex )
      {
        this.DebugMsg( string.Format( "StopRecalcTimer: {0}", ex.Message ) );
      }
    }

    private void WorkerRecalculateDocCollection ( Object self, ElapsedEventArgs e )
    {
      try
      {
        Boolean bDrainQueue = this.DrainWorkerRecalculateDocCollectionQueue();
        if( bDrainQueue )
        {
          this.TimerRecalc.Interval = 2000;
          this.RecalculateDocCollection();
        }
        else
        {
          this.TimerRecalc.Interval = 10000;
        }
      }
      catch( Exception ex )
      {
        DebugMsg( string.Format( "WorkerRecalculateDocCollection: {0}", ex.Message ) );
      }
    }

    /**************************************************************************/

    public void AddWorkerRecalculateDocCollectionQueue ()
    {
      this.NamedQueue.AddToNamedQueue( MacroscopeConstants.RecalculateDocCollection, "calc" );
    }

    /**************************************************************************/

    public Boolean DrainWorkerRecalculateDocCollectionQueue ()
    {
      Boolean bResult = false;
      try
      {
        if( this.NamedQueue.PeekNamedQueue( MacroscopeConstants.RecalculateDocCollection ) )
        {
          bResult = true;
          this.NamedQueue.DrainNamedQueueItemsAsList( MacroscopeConstants.RecalculateDocCollection );
        }
      }
      catch( InvalidOperationException ex )
      {
        this.DebugMsg( string.Format( "DrainWorkerRecalculateDocCollectionQueue: {0}", ex.Message ) );
      }
      return( bResult );
    }

    /**************************************************************************/

    public void RecalculateDocCollection ()
    {

      this.DebugMsg( string.Format( "RecalculateDocCollection: CALLED" ) );

      SemaphoreRecalc.WaitOne();

      lock( this.DocCollection )
      {

        MacroscopeAllowedHosts AllowedHosts = this.JobMaster.GetAllowedHosts();

        this.StatsUrlsInternal = 0;
        this.StatsUrlsExternal = 0;
        this.StatsUrlsSitemaps = 0;

        foreach( string UrlTarget in this.DocCollection.Keys )
        {

          MacroscopeDocument msDoc = this.GetDocument( UrlTarget );

          try
          {
            this.RecalculateHyperlinksIn( msDoc );
          }
          catch( Exception ex )
          {
            this.DebugMsg( string.Format( "RecalculateHyperlinksIn: {0}", ex.Message ) );
          }
          
          if( this.StatsHistory.ContainsKey( UrlTarget ) )
          {

            this.DebugMsg( string.Format( "RecalculateDocCollection Already Seen: {0}", UrlTarget ) );

          }
          else
          {

            this.DebugMsg( string.Format( "RecalculateDocCollection Adding: {0}", UrlTarget ) );

            this.StatsHistory.Add( UrlTarget, true );

            this.RecalculateStatsHostnames( msDoc );

            this.RecalculateStatsTitles( msDoc );

            this.RecalculateStatsDescriptions( msDoc );

            this.RecalculateStatsKeywords( msDoc );

            this.RecalculateStatsWarnings( msDoc );

            this.RecalculateStatsErrors( msDoc );

            this.RecalculateStatsChecksums( msDoc );

            this.RecalculateStatsDurations( msDoc );
            
            if( MacroscopePreferencesManager.GetAnalyzeKeywordsInText() )
            {
              this.RecalculateStatsDeepKeywordAnalysis( msDoc );
            }
            
            this.AddDocumentToSearchIndex( msDoc );

          }

          if( AllowedHosts.IsAllowed( msDoc.GetHostname() ) )
          {
            this.StatsUrlsInternal++;
          }
          else
          {
            this.StatsUrlsExternal++;
          }

          if( msDoc.GetIsSitemapXml() )
          {
            this.StatsUrlsSitemaps++;
          }

        }

      }

      SemaphoreRecalc.Release();

    }

    /** Hyperlinks In *********************************************************/

    private void RecalculateHyperlinksIn ( MacroscopeDocument msDoc )
    {

      DebugMsg( string.Format( "RecalculateHyperlinksIn: {0} :: {1}", msDoc.GetProcessHyperlinksIn(), msDoc.GetUrl() ) );

      if( msDoc.GetProcessHyperlinksIn() )
      {

        DebugMsg( string.Format( "RecalculateHyperlinksIn: PROCESSING: {0}", msDoc.GetUrl() ) );
        
        msDoc.UnsetProcessHyperlinksIn();

        foreach( MacroscopeHyperlinkOut HyperlinkOut in msDoc.GetHyperlinksOut().IterateLinks() )
        {
                  
          string Url = HyperlinkOut.GetUrlTarget();
          MacroscopeHyperlinksIn HyperlinksIn = null;

          DebugMsg( string.Format( "RecalculateHyperlinksIn: URL SOURCE: {0}", msDoc.GetUrl() ) );
          DebugMsg( string.Format( "RecalculateHyperlinksIn: URL TARGET: {0}", Url ) );

          if( Url == msDoc.GetUrl() )
          {
            DebugMsg( string.Format( "RecalculateHyperlinksIn: SELF: {0}", Url ) );
            continue;
          }
          
          if( StructHyperlinksIn.ContainsKey( Url ) )
          {
            HyperlinksIn = StructHyperlinksIn[ Url ];
          }
          else
          {
            HyperlinksIn = new MacroscopeHyperlinksIn ();
            StructHyperlinksIn.Add( Url, HyperlinksIn );
          }

          if( HyperlinksIn != null )
          {

            HyperlinksIn.Add(
              LinkType: HyperlinkOut.GetHyperlinkType(),
              Method: HyperlinkOut.GetMethod(),
              UrlOrigin: msDoc.GetUrl(),
              UrlTarget: Url,
              LinkText: HyperlinkOut.GetLinkText(),
              LinkTitle: HyperlinkOut.GetLinkTitle(),
              AltText: HyperlinkOut.GetAltText()
            );

          }
          else
          {
            DebugMsg( string.Format( "RecalculateHyperlinksIn: NULL: {0}", msDoc.GetUrl() ) );
          }

        }

      }
      else
      {
        
        DebugMsg( string.Format( "RecalculateHyperlinksIn: ALREADY PROCESSED: {0}", msDoc.GetUrl() ) );
        
      }

    }

    /** Hostnames *************************************************************/

    private void ClearStatsHostnames ()
    {
      this.StatsHostnames.Clear();
    }

    public Dictionary<string,int> GetStatsHostnamesWithCount ()
    {
      Dictionary<string,int> dicHostnames = new Dictionary<string,int> ( this.StatsHostnames.Count );
      lock( this.StatsHostnames )
      {
        foreach( string sHostname in this.StatsHostnames.Keys )
        {
          dicHostnames.Add( sHostname, this.StatsHostnames[ sHostname ] );
        }
      }
      return( dicHostnames );
    }

    public int GetStatsHostnamesCount ( string sText )
    {
      int iValue = 0;
      if( this.StatsHostnames.ContainsKey( sText ) )
      {
        iValue = this.StatsHostnames[ sText ];
      }
      return( iValue );
    }

    private void RecalculateStatsHostnames ( MacroscopeDocument msDoc )
    {
      string sUrl = msDoc.GetUrl();
      string sText = msDoc.GetHostname();

      if( ( sText != null ) && ( sText.Length > 0 ) )
      {

        sText = sText.ToLower();

        if( this.StatsHostnames.ContainsKey( sText ) )
        {
          lock( this.StatsHostnames )
          {
            this.StatsHostnames[ sText ] = this.StatsHostnames[ sText ] + 1;
          }
        }
        else
        {
          lock( this.StatsHostnames )
          {
            this.StatsHostnames.Add( sText, 1 );
          }
        }

      }

    }

    /** Titles ****************************************************************/

    private void ClearStatsTitles ()
    {
      this.StatsTitles.Clear();
    }

    public int GetStatsTitleCount ( string sText )
    {
      int iValue = 0;
      string sHashed = sText.GetHashCode().ToString();
      if( this.StatsTitles.ContainsKey( sHashed ) )
      {
        iValue = this.StatsTitles[ sHashed ];
      }
      return( iValue );
    }

    private void RecalculateStatsTitles ( MacroscopeDocument msDoc )
    {

      Boolean bProcess;

      if( msDoc.GetIsHtml() )
      {
        bProcess = true;
      }
      else
      if( msDoc.GetIsPdf() )
      {
        bProcess = true;
      }
      else
      {
        bProcess = false;
      }

      if( bProcess )
      {

        string sUrl = msDoc.GetUrl();
        string sText = msDoc.GetTitle();
        string sHashed = sText.GetHashCode().ToString();

        if( this.StatsTitles.ContainsKey( sHashed ) )
        {
          lock( this.StatsTitles )
          {
            this.StatsTitles[ sHashed ] = this.StatsTitles[ sHashed ] + 1;
          }
        }
        else
        {
          lock( this.StatsTitles )
          {
            this.StatsTitles.Add( sHashed, 1 );
          }
        }

      }

    }

    /** Descriptions **********************************************************/

    private void ClearStatsDescriptions ()
    {
      this.StatsDescriptions.Clear();
    }

    public int GetStatsDescriptionCount ( string sText )
    {
      int iValue = 0;
      string sHashed = sText.GetHashCode().ToString();
      if( this.StatsDescriptions.ContainsKey( sHashed ) )
      {
        iValue = this.StatsDescriptions[ sHashed ];
      }
      return( iValue );
    }

    private void RecalculateStatsDescriptions ( MacroscopeDocument msDoc )
    {

      Boolean bProcess;

      if( msDoc.GetIsHtml() )
      {
        bProcess = true;
      }
      else
      if( msDoc.GetIsPdf() )
      {
        bProcess = true;
      }
      else
      {
        bProcess = false;
      }

      if( bProcess )
      {

        string sUrl = msDoc.GetUrl();
        string sText = msDoc.GetDescription();
        string sHashed = sText.GetHashCode().ToString();

        if( this.StatsDescriptions.ContainsKey( sHashed ) )
        {
          lock( this.StatsDescriptions )
          {
            this.StatsDescriptions[ sHashed ] = this.StatsDescriptions[ sHashed ] + 1;
          }
        }
        else
        {
          lock( this.StatsDescriptions )
          {
            this.StatsDescriptions.Add( sHashed, 1 );
          }
        }

      }

    }

    /** Keywords **************************************************************/

    private void ClearStatsKeywords ()
    {
      this.StatsKeywords.Clear();
    }

    public int GetStatsKeywordsCount ( string sText )
    {
      int iValue = 0;
      string sHashed = sText.GetHashCode().ToString();
      if( this.StatsKeywords.ContainsKey( sHashed ) )
      {
        iValue = this.StatsKeywords[ sHashed ];
      }
      return( iValue );
    }

    private void RecalculateStatsKeywords ( MacroscopeDocument msDoc )
    {

      Boolean bProcess;

      if( msDoc.GetIsHtml() )
      {
        bProcess = true;
      }
      else
      {
        bProcess = false;
      }

      if( bProcess )
      {

        string sUrl = msDoc.GetUrl();
        string sText = msDoc.GetKeywords();
        string sHashed = sText.GetHashCode().ToString();

        if( this.StatsKeywords.ContainsKey( sHashed ) )
        {
          lock( this.StatsKeywords )
          {
            this.StatsKeywords[ sHashed ] = this.StatsKeywords[ sHashed ] + 1;
          }
        }
        else
        {
          lock( this.StatsKeywords )
          {
            this.StatsKeywords.Add( sHashed, 1 );
          }
        }
        
      }

    }

    /** Warnings ****************************************************************/

    private void ClearStatsWarnings ()
    {
      lock( this.StatsWarnings )
      {
        this.StatsWarnings.Clear();
      }
    }

    public Dictionary<string,int> GetStatsWarningsCount ()
    {
      Dictionary<string,int> dicStats = new Dictionary<string,int> ( this.StatsWarnings.Count );
      lock( this.StatsWarnings )
      {
        foreach( string sKey in this.StatsWarnings.Keys )
        {
          dicStats.Add( sKey, this.StatsWarnings[ sKey ] );
        }
      }
      return( dicStats );
    }

    private void RecalculateStatsWarnings ( MacroscopeDocument msDoc )
    {

      string sErrorCondition = msDoc.GetErrorCondition();
      HttpStatusCode iStatusCode = msDoc.GetStatusCode();

      if( ( ( int )iStatusCode >= 300 ) && ( ( int )iStatusCode <= 399 ) )
      {

        string sText = string.Format( "{0} ({1})", iStatusCode, sErrorCondition );

        DebugMsg( string.Format( "RecalculateStatsWarnings: {0}", sText ) );

        lock( this.StatsWarnings )
        {

          if( this.StatsWarnings.ContainsKey( sText ) )
          {
            this.StatsWarnings[ sText ] = this.StatsWarnings[ sText ] + 1;
          }
          else
          {
            this.StatsWarnings.Add( sText, 1 );
          }

        }

      }

    }

    /** Errors ****************************************************************/

    private void ClearStatsErrors ()
    {
      lock( this.StatsErrors )
      {
        this.StatsErrors.Clear();
      }
    }

    public Dictionary<string,int> GetStatsErrorsCount ()
    {
      Dictionary<string,int> dicStats = new Dictionary<string,int> ( this.StatsErrors.Count );
      lock( this.StatsErrors )
      {
        foreach( string sKey in this.StatsErrors.Keys )
        {
          dicStats.Add( sKey, this.StatsErrors[ sKey ] );
        }
      }
      return( dicStats );
    }

    private void RecalculateStatsErrors ( MacroscopeDocument msDoc )
    {

      string sErrorCondition = msDoc.GetErrorCondition();
      HttpStatusCode iStatusCode = msDoc.GetStatusCode();

      if( ( ( int )iStatusCode >= 400 ) && ( ( int )iStatusCode <= 599 ) )
      {

        string sText = string.Format( "{0} ({1})", iStatusCode, sErrorCondition );

        DebugMsg( string.Format( "RecalculateStatsErrors: {0}", sText ) );

        lock( this.StatsErrors )
        {

          if( this.StatsErrors.ContainsKey( sText ) )
          {
            this.StatsErrors[ sText ] = this.StatsErrors[ sText ] + 1;
          }
          else
          {
            this.StatsErrors.Add( sText, 1 );
          }

        }

      }

    }

    /** Checksums *************************************************************/

    private void ClearStatsChecksums ()
    {
      lock( this.StatsChecksums )
      {
        this.StatsChecksums.Clear();
      }
    }

    public int GetStatsChecksumCount ( string Checksum )
    {
      int Count = 0;
      lock( this.StatsChecksums )
      {
        if( this.StatsChecksums.ContainsKey( Checksum ) )
        {
          Count = this.StatsChecksums[ Checksum ];
        }
      }
      return( Count );
    }

    public Dictionary<string,int> GetStatsChecksumsCount ()
    {
      Dictionary<string,int> dicStats = new Dictionary<string,int> ( this.StatsChecksums.Count );
      lock( this.StatsChecksums )
      {
        foreach( string sKey in this.StatsChecksums.Keys )
        {
          dicStats.Add( sKey, this.StatsChecksums[ sKey ] );
        }
      }
      return( dicStats );
    }

    private void RecalculateStatsChecksums ( MacroscopeDocument msDoc )
    {

      string sChecksum = msDoc.GetChecksum();

      if( sChecksum.Length > 0 )
      {

        DebugMsg( string.Format( "RecalculateStatsChecksums: {0}", sChecksum ) );

        lock( this.StatsChecksums )
        {

          if( this.StatsChecksums.ContainsKey( sChecksum ) )
          {
            this.StatsChecksums[ sChecksum ] = this.StatsChecksums[ sChecksum ] + 1;
          }
          else
          {
            this.StatsChecksums.Add( sChecksum, 1 );
          }

        }

      }

    }

    /** Average Duration ******************************************************/
    
    private void ClearStatsDurations ()
    {
      lock( this.StatsDurations )
      {
        this.StatsDurations.Clear();
      }
    }

    public decimal GetStatsDurationAverage ()
    {
      
      decimal Average = 0;
      decimal Maximus = 0;
      int Count = 0;
      
      if( this.StatsDurations.Count > 0 )
      {
        
        lock( this.StatsDurations )
        {
          foreach( string Url in this.StatsDurations.Keys )
          {
            Count++;
            Maximus += this.StatsDurations[ Url ];
          }
        }
      
        if( Count > 0 )
        {
          Average = Maximus / Count;
        }
        
      }
      
      return( Average );
    }

    public decimal GetStatsDurationsFastest ()
    {
      decimal Fastest = -1; 
      if( this.StatsDurations.Count > 0 )
      {   
        lock( this.StatsDurations )
        {
          foreach( string Url in this.StatsDurations.Keys )
          {
            if( Fastest == -1 )
            {
              Fastest = this.StatsDurations[ Url ];
            }
            else
            {
              if( this.StatsDurations[ Url ] <= Fastest )
              {
                Fastest = this.StatsDurations[ Url ];
              }
            }
          }
        }
      } 
      return( Fastest );
    }

    public decimal GetStatsDurationsSlowest ()
    {
      decimal Slowest = -1; 
      if( this.StatsDurations.Count > 0 )
      {   
        lock( this.StatsDurations )
        {
          foreach( string Url in this.StatsDurations.Keys )
          {
            if( Slowest == -1 )
            {
              Slowest = this.StatsDurations[ Url ];
            }
            else
            {
              if( this.StatsDurations[ Url ] >= Slowest )
              {
                Slowest = this.StatsDurations[ Url ];
              }
            }
          }
        }
      } 
      return( Slowest );
    }

    private void RecalculateStatsDurations ( MacroscopeDocument msDoc )
    {
      string Url = msDoc.GetUrl();
      lock( this.StatsDurations )
      {
        if( this.StatsDurations.ContainsKey( Url ) )
        {
          this.StatsDurations[ Url ] = msDoc.GetDurationInSeconds();
        }
        else
        {
          this.StatsDurations.Add( Url, msDoc.GetDurationInSeconds() );
        }
      }
      ;
    }

    /** Deep Keyword Analysis *************************************************/

    private void ClearStatsDeepKeywordAnalysis ()
    {
      lock( this.StatsDeepKeywordAnalysis )
      {
        for( int i = 0 ; i <= 3 ; i++ )
        {
          lock( this.StatsDeepKeywordAnalysis[i] )
          {
            this.StatsDeepKeywordAnalysis[ i ].Clear();
          }
        }
      }
    }

    private void RecalculateStatsDeepKeywordAnalysis ( MacroscopeDocument msDoc )
    {

      Boolean bProceed = false;

      if( msDoc.GetIsHtml() )
      {
        bProceed = true;
      }
      else
      if( msDoc.GetIsPdf() )
      {
        bProceed = true;
      }
      
      if( bProceed )
      {
        
        string sLang = msDoc.GetLang();
        
        if( sLang != null )
        {
          DebugMsg( string.Format( "RecalculateStatsDeepKeywordAnalysis: GetLang {0}", msDoc.GetLang() ) );
          if( Regex.IsMatch( msDoc.GetLang(), "^(x-default|en|fr|de|it|es|po)", RegexOptions.IgnoreCase ) )
          {
            lock( this.StatsDeepKeywordAnalysis )
            {
              for( int i = 0 ; i <= 3 ; i++ )
              {
                this.AnalyzeKeywords.Analyze(
                  Text: msDoc.GetBodyText(),
                  Terms: this.StatsDeepKeywordAnalysis[ i ],
                  Words: i + 1
                );
              }
            }
          }
        }
      
      }
      
    }

    public Dictionary<string,int> GetDeepKeywordAnalysisAsDictonary ( int Words )
    {
      
      int iWordsOffset = Words - 1;
      
      DebugMsg( string.Format( "GetDeepKeywordAnalysisAsDictonary: Words: {0}", Words ) );
      DebugMsg( string.Format( "GetDeepKeywordAnalysisAsDictonary: iWordsOffset: {0}", iWordsOffset ) );
      
      DebugMsg( string.Format( "GetDeepKeywordAnalysisAsDictonary: this.StatsDeepKeywordAnalysis: {0}", this.StatsDeepKeywordAnalysis[ iWordsOffset ].Count ) );
      
            
      Dictionary<string,int> Terms = new Dictionary<string,int> ( this.StatsDeepKeywordAnalysis[ iWordsOffset ].Count );
      
      
      DebugMsg( string.Format( "GetDeepKeywordAnalysisAsDictonary: Terms: {0}", Terms.Count ) );
      
      
      lock( this.StatsDeepKeywordAnalysis[iWordsOffset] )
      {
        foreach( string sTerm in this.StatsDeepKeywordAnalysis[iWordsOffset].Keys )
        {
          Terms.Add( sTerm, this.StatsDeepKeywordAnalysis[ iWordsOffset ][ sTerm ] );
        }
      }
      
      return( Terms );
      
    }

    /** Search Index **********************************************************/

    public MacroscopeSearchIndex GetSearchIndex ()
    {
      return( this.SearchIndex );
    }

    private void AddDocumentToSearchIndex ( MacroscopeDocument msDoc )
    {
      if( msDoc.GetIsHtml() )
      {
        this.SearchIndex.AddDocumentToIndex( msDoc );
      }
    }

    /**************************************************************************/

  }

}
