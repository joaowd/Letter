﻿using System;

using Foundation;
using LetterApp.Core.Models;
using LetterApp.iOS.Helpers;
using UIKit;

namespace LetterApp.iOS.Views.Register.Cells
{
    public partial class AgreementCell : UITableViewCell
    {
        bool _userAgreed;
        public static readonly NSString Key = new NSString("AgreementCell");
        public static readonly UINib Nib = UINib.FromName("AgreementCell", NSBundle.MainBundle);
        protected AgreementCell(IntPtr handle) : base(handle) {}

        public void Configure(string agreement, bool userAgreed)
        {
            _userAgreed = userAgreed;
            ButtonState();
            _label.AttributedText = StringExtensions.GetHTMLFormattedText(agreement, fontSize: 3);

            _button.TouchUpInside -= OnButton_TouchUpInside;
            _button.TouchUpInside += OnButton_TouchUpInside;
        }

        private void OnButton_TouchUpInside(object sender, EventArgs e)
        {
            _userAgreed = !_userAgreed;
            ButtonState();
        }

        private void ButtonState()
        {
            _button.SetImage(_userAgreed ? UIImage.FromBundle("checkbox_full") : UIImage.FromBundle("checkbox_empty"), UIControlState.Normal);
        }
    }
}
