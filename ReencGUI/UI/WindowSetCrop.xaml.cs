using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ReencGUI.UI
{
    /// <summary>
    /// Logika interakcji dla klasy WindowSetCrop.xaml
    /// </summary>
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
                if (ignoreTextChanged) return;
                dontChangeText = true;
                ValidateCropStringAndChangeSliders(Input_CropArg.InputField.Text);
                dontChangeText = false;
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

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowUtil.SetWindowDarkMode(this);
        }

        void GrabImage()
        {
            var media = caller.GetPreviewVideoMedia();
            if (media != null)
            {
                var bmp = FFMPEG.ExtractThumbnail(media.fileName, "00");
                if (bmp != null)
                {
                    Image_Preview.Source = bmp;
                    resX = bmp.PixelWidth;
                    resY = bmp.PixelHeight;
                } else
                {
                    this.Close();
                }
            } else
            {
                this.Close();
            }
        }

        void Redraw()
        {
            Canvas_Preview.Children.Clear();

            Size canvasRenderSize = Canvas_Preview.RenderSize;
            Size imageRenderSize = Image_Preview.RenderSize;

            int imageRenderX = canvasRenderSize.Width == imageRenderSize.Width ? 0 : (int)((canvasRenderSize.Width - imageRenderSize.Width) / 2);
            int imageRenderY = canvasRenderSize.Height == imageRenderSize.Height ? 0 : (int)((canvasRenderSize.Height - imageRenderSize.Height) / 2);

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
                    Width = imageRenderSize.Width * leftPosition,
                    Height = imageRenderSize.Height
                };
                Canvas_Preview.Children.Add(leftCropRect);
            }

            if (rightPosition < 1)
            {
                int rCropW = (int)(imageRenderSize.Width * (1.0-rightPosition));
                Rectangle rightCropRect = new Rectangle
                {
                    Fill = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    Margin = new Thickness(imageRenderX + imageRenderSize.Width - rCropW, imageRenderY, 0, 0),
                    Width = rCropW,
                    Height = imageRenderSize.Height
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
                    Width = imageRenderSize.Width,
                    Height = imageRenderSize.Height * topPosition
                };
                Canvas_Preview.Children.Add(topCropRect);
            }
            if (bottomPosition < 1)
            {
                int bCropH = (int)(imageRenderSize.Height * (1.0 - bottomPosition));
                Rectangle bottomCropRect = new Rectangle
                {
                    Fill = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    Margin = new Thickness(imageRenderX, imageRenderY + imageRenderSize.Height - bCropH, 0, 0),
                    Width = imageRenderSize.Width,
                    Height = bCropH
                };
                Canvas_Preview.Children.Add(bottomCropRect);
            }

            Rectangle innerImageRect = new Rectangle
            {
                Stroke = Brushes.Red,
                Margin = new Thickness(imageRenderX, imageRenderY, 0, 0),
                Width = imageRenderSize.Width,
                Height = imageRenderSize.Height
            };
            //Canvas_Preview.Children.Add(innerImageRect);

            Rectangle innerCropRect = new Rectangle
            {
                Stroke = Brushes.Lime,
                Margin = new Thickness(
                    imageRenderX + imageRenderSize.Width * leftPosition,
                    imageRenderY + imageRenderSize.Height * topPosition,
                    0,
                    0),
                Width = imageRenderSize.Width * (rightPosition - leftPosition),
                Height = imageRenderSize.Height * (bottomPosition - topPosition)
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
            if (!dontChangeText)
            {
                ignoreTextChanged = true;
                Input_CropArg.InputField.Text = MakeCropString();
                ignoreTextChanged = false;
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (initPassed)
            {
                Redraw();
            }
        }

        private void Slider_Left_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (initPassed)
            {
                Redraw();
                UpdateCropInputField();
            }
        }

        private void Slider_Bottom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (initPassed)
            {
                Redraw();
                UpdateCropInputField();
            }
        }

        private void Slider_Right_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (initPassed)
            {
                Redraw();
                UpdateCropInputField();
            }
        }

        private void Slider_Top_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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
