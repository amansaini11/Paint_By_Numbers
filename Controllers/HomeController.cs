using Microsoft.AspNetCore.Mvc;
using OpenCvSharp;
using Paint_By_Numbers.Models;
using System;
using System.Diagnostics;
using System.Drawing;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace Paint_By_Numbers.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult UploadImage(ImageModel model)
        {
            if (model.ImagePath != null && model.ImagePath.Length > 0)
            {
                using (var stream = model.ImagePath.OpenReadStream())
                {
                    byte[] imageData;
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        imageData = memoryStream.ToArray();
                    }
                    Mat originalImage = Cv2.ImDecode(imageData, ImreadModes.Color);

                    if (originalImage.Empty())
                    {
                        ViewBag.ErrorMessage = "Error: Unable to load the image.";
                        return View("Index");
                    }

                    string originalImagePath = Path.Combine("wwwroot", "uploads", "original_image.jpg");
                    Cv2.ImWrite(originalImagePath, originalImage);
                    ViewBag.OriginalImagePath = originalImagePath;

                    Mat whiteBackground = Mat.Zeros(originalImage.Size(), MatType.CV_8UC3) + new Scalar(255, 255, 255);

                    Mat whiteBackgroundGray = new Mat();
                    Cv2.CvtColor(whiteBackground, whiteBackgroundGray, ColorConversionCodes.BGR2GRAY);

                    string whiteBackgroundImagePath = Path.Combine("wwwroot", "uploads", "white_background.jpg");
                    Cv2.ImWrite(whiteBackgroundImagePath, whiteBackground);
                    ViewBag.WhiteBackgroundImagePath = whiteBackgroundImagePath;

                    Mat gray = new Mat();
                    Cv2.CvtColor(originalImage, gray, ColorConversionCodes.BGR2GRAY);

                    Mat thresholded = new Mat();
                    Cv2.Threshold(gray, thresholded, 127, 255, ThresholdTypes.Binary);

                    Point[][] contours;
                    HierarchyIndex[] hierarchy;
                    Cv2.FindContours(thresholded, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    List<ColorInfo> colorInfoList = new List<ColorInfo>();

                    for (int i = 0; i < contours.Length; i++)
                    {
                        Rect boundingRect = Cv2.BoundingRect(contours[i]);

                        Cv2.DrawContours(whiteBackground, contours, i, Scalar.Black, 2);

                        int centerX = boundingRect.X + boundingRect.Width / 2;
                        int centerY = boundingRect.Y + boundingRect.Height / 2;

                        int margin = 5;
                        Cv2.PutText(whiteBackground, (i + 1).ToString(), new Point(centerX - margin, centerY + margin), HersheyFonts.HersheySimplex, 1, Scalar.Red);

                        Scalar avgColor = Cv2.Mean(originalImage.SubMat(boundingRect));

                        colorInfoList.Add(new ColorInfo
                        {
                            Number = i + 1,
                            Color = Color.FromArgb((int)avgColor.Val0, (int)avgColor.Val1, (int)avgColor.Val2)
                        });
                    }


                    TempData["ColorInfoList"] = colorInfoList;

                    string processedImagePath = Path.Combine("wwwroot", "uploads", "processed_image_with_numbers.jpg");
                    Cv2.ImWrite(processedImagePath, whiteBackground);
                    ViewBag.ProcessedImagePath = processedImagePath;

                    string colorContainerImagePath = Path.Combine("wwwroot", "uploads", "color_container.jpg");
                    CreateColorContainerImage(colorContainerImagePath, colorInfoList);
                    ViewBag.ColorContainerImagePath = colorContainerImagePath;
                }
            }

            return View("Index");
        }


        private void CreateColorContainerImage(string imagePath, List<ColorInfo> colorInfoList)
        {
            Mat colorContainer = Mat.Zeros(new Size(200, colorInfoList.Count * 50), MatType.CV_8UC3) + new Scalar(255, 255, 255);

            for (int i = 0; i < colorInfoList.Count; i++)
            {
                Scalar colorScalar = new Scalar(colorInfoList[i].Color.B, colorInfoList[i].Color.G, colorInfoList[i].Color.R);
                Cv2.Rectangle(colorContainer, new Rect(0, i * 50, 200, 50), colorScalar, -1);
                Cv2.PutText(colorContainer, $"Color {colorInfoList[i].Number}", new Point(10, (i * 50) + 30), HersheyFonts.HersheySimplex, 1, Scalar.Black);
            }

            Cv2.ImWrite(imagePath, colorContainer);
        }
    }
}

