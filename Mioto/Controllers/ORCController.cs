using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tesseract;

namespace Mioto.Controllers
{
    public class ORCController : Controller
    {
        [HttpPost]
        public ActionResult UploadImage(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var path = Path.Combine(Server.MapPath("~/UploadedImages"), fileName);
                    file.SaveAs(path);

                    string ocrResult = PerformOCR(path);

                    ViewBag.OCRResult = ocrResult;
                    return View();
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Error: " + ex.Message;
                }
            }
            else
            {
                ViewBag.Message = "No file selected.";
            }

            return View();
        }

        private string PerformOCR(string imagePath)
        {
            string result = "";

            try
            {
                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(imagePath))
                    {
                        using (var page = engine.Process(img))
                        {
                            result = page.GetText();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = "Error during OCR: " + ex.Message;
            }

            return result;
        }
    }
}