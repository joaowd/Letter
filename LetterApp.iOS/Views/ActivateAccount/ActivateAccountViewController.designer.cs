// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace LetterApp.iOS.Views.ActivateAccount
{
    [Register ("ActivateAccountViewController")]
    partial class ActivateAccountViewController
    {
        [Outlet]
        UIKit.UILabel _activateLabel { get; set; }


        [Outlet]
        UIKit.UIView _backgroundView { get; set; }


        [Outlet]
        UIKit.UIButton _button { get; set; }


        [Outlet]
        UIKit.UIView _buttonView { get; set; }


        [Outlet]
        UIKit.UIButton _closeButon { get; set; }


        [Outlet]
        UIKit.UIButton _requestCodeButton { get; set; }


        [Outlet]
        UIKit.UITextField _textField { get; set; }


        [Outlet]
        UIKit.UILabel _titleLabel { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (_activateLabel != null) {
                _activateLabel.Dispose ();
                _activateLabel = null;
            }

            if (_backgroundView != null) {
                _backgroundView.Dispose ();
                _backgroundView = null;
            }

            if (_button != null) {
                _button.Dispose ();
                _button = null;
            }

            if (_buttonView != null) {
                _buttonView.Dispose ();
                _buttonView = null;
            }

            if (_closeButon != null) {
                _closeButon.Dispose ();
                _closeButon = null;
            }

            if (_requestCodeButton != null) {
                _requestCodeButton.Dispose ();
                _requestCodeButton = null;
            }

            if (_textField != null) {
                _textField.Dispose ();
                _textField = null;
            }

            if (_titleLabel != null) {
                _titleLabel.Dispose ();
                _titleLabel = null;
            }
        }
    }
}