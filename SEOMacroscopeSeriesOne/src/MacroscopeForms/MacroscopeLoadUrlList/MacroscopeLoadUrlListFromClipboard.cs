﻿/*

  This file is part of SEOMacroscope.

  Copyright 2020 Jason Holland.

  The GitHub repository may be found at:

    https://github.com/nazuke/SEOMacroscope

  SEOMacroscope is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  SEOMacroscope is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with SEOMacroscope.  If not, see <http://www.gnu.org/licenses/>.

*/

using System;
using System.Drawing;
using System.Windows.Forms;

namespace SEOMacroscope
{

  /// <summary>
  /// Description of MacroscopeLoadUrlListFromClipboard.
  /// </summary>

  public partial class MacroscopeLoadUrlListFromClipboard : Form
  {

    /**************************************************************************/

    public MacroscopeLoadUrlListFromClipboard ()
    {

      InitializeComponent(); // The InitializeComponent() call is required for Windows Forms designer support.

      this.textBoxUrls.MaxLength = 1024 * 1024; // 1MB

      this.textBoxUrls.KeyUp += this.CallbackPatternsTextKeyUp;

    }

    /**************************************************************************/

    private void CallbackPatternsTextKeyUp ( object sender, KeyEventArgs e )
    {

      TextBox PatternsTextBox = ( TextBox ) sender;

      if( e.Control && ( e.KeyCode == Keys.A ) )
      {

        PatternsTextBox.SelectAll();
        PatternsTextBox.Focus();

      }

    }

    /**************************************************************************/

    public string GetUrlsText ()
    {
      return ( this.textBoxUrls.Text );
    }

    /**************************************************************************/

  }

}
