// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LetterApp.iOS.Views.Chat.Cells
{
	[Register ("ImageWithUserCell")]
	partial class ImageWithUserCell
	{
		[Outlet]
		UIKit.UILabel _dateLabel { get; set; }

		[Outlet]
		UIKit.UIView _dateView { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint _dateViewHeightConstraint { get; set; }

		[Outlet]
		UIKit.UIView _dividerLeftView { get; set; }

		[Outlet]
		UIKit.UIView _dividerRightView { get; set; }

		[Outlet]
		UIKit.UIImageView _imageView { get; set; }

		[Outlet]
		UIKit.UILabel _nameLabel { get; set; }

		[Outlet]
		UIKit.UIButton _pictureButton { get; set; }

		[Outlet]
		UIKit.UIImageView _pictureImage { get; set; }

		[Outlet]
		UIKit.UIView _presenceView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (_imageView != null) {
				_imageView.Dispose ();
				_imageView = null;
			}

			if (_nameLabel != null) {
				_nameLabel.Dispose ();
				_nameLabel = null;
			}

			if (_pictureButton != null) {
				_pictureButton.Dispose ();
				_pictureButton = null;
			}

			if (_pictureImage != null) {
				_pictureImage.Dispose ();
				_pictureImage = null;
			}

			if (_presenceView != null) {
				_presenceView.Dispose ();
				_presenceView = null;
			}

			if (_dateView != null) {
				_dateView.Dispose ();
				_dateView = null;
			}

			if (_dateViewHeightConstraint != null) {
				_dateViewHeightConstraint.Dispose ();
				_dateViewHeightConstraint = null;
			}

			if (_dateLabel != null) {
				_dateLabel.Dispose ();
				_dateLabel = null;
			}

			if (_dividerLeftView != null) {
				_dividerLeftView.Dispose ();
				_dividerLeftView = null;
			}

			if (_dividerRightView != null) {
				_dividerRightView.Dispose ();
				_dividerRightView = null;
			}
		}
	}
}
