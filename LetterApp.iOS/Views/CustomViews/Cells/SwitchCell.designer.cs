// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace LetterApp.iOS.Views.CustomViews.Cells
{
    [Register ("SwitchCell")]
    partial class SwitchCell
    {
        [Outlet]
        UIKit.UILabel _label { get; set; }


        [Outlet]
        UIKit.UISwitch _switch { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (_label != null) {
                _label.Dispose ();
                _label = null;
            }

            if (_switch != null) {
                _switch.Dispose ();
                _switch = null;
            }
        }
    }
}