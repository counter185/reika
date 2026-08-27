using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using reika.Core;
using System.Text.RegularExpressions;

namespace reika.Linux.UI
{
    public partial class WindowSetCrop : Window
    {
        WindowCreateFile caller;
        int resX;
        int resY;
        bool initPassed = false;

        bool ignoreTextChanged = false;
        bool dontChangeText = false;

        public WindowSetCrop(WindowCreateFile caller)
        {
            this.caller = caller;
            InitializeComponent();

            initPassed = true;
            //todo:can't close before showdialog
            GrabImage();
            Redraw();
            UpdateCropInputField();

            if (ValidateCropStringAndChangeSliders(caller.Input_Crop.InputField.Text))
            {
                Input_CropArg.InputField.Text = caller.Input_Crop.InputField.Text;
            }

            Input_CropArg.InputField.TextChanged += (s, e) =>
            {
                if (Input_CropArg.InputField.IsFocused)
                {
                    ValidateCropStringAndChangeSliders(Input_CropArg.InputField.Text);
                }
            };
        }

        private bool ValidateCropStringAndChangeSliders(string s)
        {
            Regex regex = new Regex(@"^(\d+):(\d+):(\d+):(\d+)$");
            Match match = regex.Match(s);
            if (match.Success)
            {
                int cropWidth = int.Parse(match.Groups[1].Value);
                int cropHeight = int.Parse(match.Groups[2].Value);
                int cropLeft = int.Parse(match.Groups[3].Value);
                int cropTop = int.Parse(match.Groups[4].Value);
                double left = (double)cropLeft / resX;
                double top = (double)cropTop / resY;
                double right = (double)(cropLeft + cropWidth) / resX;
                double bottom = (double)(cropTop + cropHeight) / resY;
                Slider_Top.Value = left;
                Slider_Bottom.Value = right;
                Slider_Left.Value = 1.0 - top;
                Slider_Right.Value = 1.0 - bottom;
                return true;
            }
            return false;
        }

        void GrabImage()
        {
            var media = caller.GetPreviewVideoMedia();
            if (media != null)
            {
                var bmp = LinuxUtils.LoadToMemFromUri(ThumbnailUtil.FFMPEGExtractThumbnail(media.fileName, "00"));
                if (bmp != null)
                {
                    Image_Preview.Source = bmp;
                    resX = bmp.PixelSize.Width;
                    resY = bmp.PixelSize.Height;
                }
                else
                {
                    this.Close();
                }
            }
            else
            {
                this.Close();
            }
        }

        void Redraw()
        {
            Canvas_Preview.Children.Clear();

            double canvasW = Canvas_Preview.Bounds.Width, canvasH = Canvas_Preview.Bounds.Height;
            double imageRenderW = Image_Preview.Bounds.Width, imageRenderH = Image_Preview.Bounds.Height;

            int imageRenderX = canvasW == imageRenderW ? 0 : (int)((canvasW - imageRenderW) / 2);
            int imageRenderY = canvasH == imageRenderH ? 0 : (int)((canvasH - imageRenderH) / 2);

            double leftPosition = Slider_Top.Value;
            double rightPosition = Slider_Bottom.Value;
            if (rightPosition < leftPosition)
            {
                double t = rightPosition;
                rightPosition = leftPosition;
                leftPosition = t;
            }

            if (leftPosition > 0)
            {
                Rectangle leftCropRect = new Rectangle
                {
                    Fill = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    Margin = new Thickness(imageRenderX, imageRenderY, 0, 0),
                    Width = imageRenderW * leftPosition,
                    Height = imageRenderH
                };
                Canvas_Preview.Children.Add(leftCropRect);
            }

            if (rightPosition < 1)
            {
                int rCropW = (int)(imageRenderW * (1.0 - rightPosition));
                Rectangle rightCropRect = new Rectangle
                {
                    Fill = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    Margin = new Thickness(imageRenderX + imageRenderW - rCropW, imageRenderY, 0, 0),
                    Width = rCropW,
                    Height = imageRenderH
                };
                Canvas_Preview.Children.Add(rightCropRect);
            }

            double topPosition = 1.0 - Slider_Left.Value;
            double bottomPosition = 1.0 - Slider_Right.Value;
            if (bottomPosition < topPosition)
            {
                double t = bottomPosition;
                bottomPosition = topPosition;
                topPosition = t;
            }
            if (topPosition > 0)
            {
                Rectangle topCropRect = new Rectangle
                {
                    Fill = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    Margin = new Thickness(imageRenderX, imageRenderY, 0, 0),
                    Width = imageRenderW,
                    Height = imageRenderH * topPosition
                };
                Canvas_Preview.Children.Add(topCropRect);
            }
            if (bottomPosition < 1)
            {
                int bCropH = (int)(imageRenderH * (1.0 - bottomPosition));
                Rectangle bottomCropRect = new Rectangle
                {
                    Fill = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    Margin = new Thickness(imageRenderX, imageRenderY + imageRenderH - bCropH, 0, 0),
                    Width = imageRenderW,
                    Height = bCropH
                };
                Canvas_Preview.Children.Add(bottomCropRect);
            }

            Rectangle innerImageRect = new Rectangle
            {
                Stroke = Brushes.Red,
                Margin = new Thickness(imageRenderX, imageRenderY, 0, 0),
                Width = imageRenderW,
                Height = imageRenderH,
                StrokeThickness = 1
            };
            //Canvas_Preview.Children.Add(innerImageRect);

            Rectangle innerCropRect = new Rectangle
            {
                Stroke = Brushes.Lime,
                Margin = new Thickness(
                    imageRenderX + imageRenderW * leftPosition,
                    imageRenderY + imageRenderH * topPosition,
                    0,
                    0),
                Width = imageRenderW * (rightPosition - leftPosition),
                Height = imageRenderH * (bottomPosition - topPosition),
                StrokeThickness = 1
            };
            Canvas_Preview.Children.Add(innerCropRect);

        }

        string MakeCropString()
        {
            double left = Slider_Top.Value;
            double right = Slider_Bottom.Value;
            if (right < left)
            {
                double t = right;
                right = left;
                left = t;
            }
            double top = 1.0 - Slider_Left.Value;
            double bottom = 1.0 - Slider_Right.Value;
            if (bottom < top)
            {
                double t = bottom;
                bottom = top;
                top = t;
            }

            int cropLeft = (int)(left * resX);
            int cropTop = (int)(top * resY);
            int cropWidth = (int)((right - left) * resX);
            int cropHeight = (int)((bottom - top) * resY);

            return $"{cropWidth}:{cropHeight}:{cropLeft}:{cropTop}";
        }

        public void UpdateCropInputField()
        {
            if (!dontChangeText && !ignoreTextChanged)
            {
                ignoreTextChanged = true;
                Input_CropArg.InputField.Text = MakeCropString();
                ignoreTextChanged = false;
            }
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            if (initPassed)
            {
                Redraw();
            }
        }

        private void SliderMoved(object sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (initPassed)
            {
                Redraw();
                UpdateCropInputField();
            }
        }

        private void Button_Apply_Click(object sender, RoutedEventArgs e)
        {
            caller.Input_Crop.InputField.Text = MakeCropString();
            Close();
        }
    }
}