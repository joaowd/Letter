﻿using System;

using Foundation;
using UIKit;

namespace LetterApp.iOS.Views.Chat.Cells
{
    public partial class LabelCell : UITableViewCell
    {
        public static readonly NSString Key = new NSString("LabelCell");
        public static readonly UINib Nib = UINib.FromName("LabelCell", NSBundle.MainBundle);
        protected LabelCell(IntPtr handle) : base(handle){}

        public void Configure()
        {

        }
    }
}
