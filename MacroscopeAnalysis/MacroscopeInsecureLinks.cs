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
using System.Text.RegularExpressions;

namespace SEOMacroscope
{

  /// <summary>
  /// Analayze possibly insecure links within a document.
  /// </summary>

  public class MacroscopeInsecureLinks : Macroscope
  {

    /**************************************************************************/

    public MacroscopeInsecureLinks ()
    {
      this.SuppressDebugMsg = false;
    }

    /**************************************************************************/

    public void Analyze ( MacroscopeDocument msDoc )
    {
      if( msDoc.GetIsSecureUrl() )
      {
        Dictionary<string,MacroscopeOutlink> DocDic = msDoc.GetOutlinks();
        foreach( string sUrl in DocDic.Keys )
        {
          if( Regex.IsMatch( sUrl, "^http://", RegexOptions.IgnoreCase ) )
          {
            msDoc.AddInsecureLink( sUrl );
          }
        }
      }
    }

    /**************************************************************************/

  }

}