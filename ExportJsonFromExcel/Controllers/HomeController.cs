﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using SugarUtilities;
using ExportJsonFromExcel.Models;
using System.Data;

namespace ExportJsonFromExcel.Controllers
{
    public class HomeController : BaseController
    {
        private const string DataTableCacheName = "DataExcelCache";
        public HomeController()
        {

        }
        public ActionResult Index()
        {
            return View();
        }

        #region export json
        [HttpGet]
        public ActionResult ExportJson()
        {
            if (Request["clearcache"] != null)
            {

                System.Web.Caching.Cache _Cache = HttpContext.Cache;
                _Cache.Remove(DataTableCacheName);
            }
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExportJson(HttpPostedFileBase file)
        {
            try
            {

                TempData["ShowModal"] = false;

                if (System.IO.Path.GetExtension(file.FileName) != ".xlsx")
                {
                    ModelState.AddModelError("", "File không hợp lệ.");
                    return View();
                }
                try
                {
                    if (!System.IO.Directory.Exists(HostingEnvironment.MapPath(Global.ExcelDirectory)))
                    {
                        System.IO.Directory.CreateDirectory(HostingEnvironment.MapPath(Global.ExcelDirectory));
                    }
                }
                catch
                {

                    throw new Exception("Lỗi khi khởi tạo đường dẫn.");
                }

                string filePath = System.IO.Path.Combine(HostingEnvironment.MapPath(Global.ExcelDirectory), file.FileName);
                file.SaveAs(filePath);

                DataTable dataTable = ExcelRepos.ReadFile(filePath);

                System.Web.Caching.Cache _Cache = HttpContext.Cache;
                _Cache.Remove(DataTableCacheName);
                _Cache.Insert(DataTableCacheName, dataTable, null, DateTime.Now.AddMinutes(60), TimeSpan.Zero);

                ViewData.Model = dataTable;

                ViewBag.Select = new SelectList(JsonObjectCustom.GetAllProperties(), "Key", "Value", "");
            }
            catch (Exception)
            {
                throw;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitSelectedColumn(FormCollection collection)
        {
            System.Web.Caching.Cache _Cache = HttpContext.Cache;

            if (_Cache[DataTableCacheName] != null)
            {
                var dataTable = _Cache.Get(DataTableCacheName) as DataTable;
                var column = dataTable.Columns.Count;

                List<string> dsUserSelectedIndex = new List<string>();
                for (int i = 1; i <= column; i++)
                {
                    if (!string.IsNullOrEmpty(collection[i.ToString()]))
                    {
                        dsUserSelectedIndex.Add(i.ToString());
                    }
                }

                if (dsUserSelectedIndex.Any())
                {
                    var dt = dataTable.Copy();
                    var removeColumn = dataTable.Columns.Cast<DataColumn>().Where(c => dsUserSelectedIndex.All(a => a != c.ColumnName));

                    foreach (DataColumn col in removeColumn)
                    {
                        dt.Columns.Remove(col.ColumnName);
                    }

                    foreach (var item in dsUserSelectedIndex)
                    {
                        dt.Columns[item].ColumnName = collection[item];
                    }

                    dt.Rows[0].Delete();
                    dt.AcceptChanges();

                    string json = JsonRepos.JsonPrettify(JsonRepos.ConvertFromDataTable(dt));
                    TempData["Json"] = json;

                    string fileName = DateTime.Now.ToString("ddMMyyyyhhmmss") + ".json";
                    TempData["Filename"] = fileName;

                    try
                    {
                        if (!System.IO.Directory.Exists(HostingEnvironment.MapPath(Global.JsonDirectory)))
                        {
                            System.IO.Directory.CreateDirectory(HostingEnvironment.MapPath(Global.JsonDirectory));
                        }
                    }
                    catch
                    {

                        throw new Exception("Lỗi khi khởi tạo đường dẫn.");
                    }

                    System.IO.File.WriteAllText(System.IO.Path.Combine(HostingEnvironment.MapPath(Global.JsonDirectory), fileName), json);

                }

            }
            TempData["ShowModal"] = false;
            return RedirectToAction("ExportJson");
        }

        #endregion

        #region export product code to one line
        public ViewResult ExportProductCode()
        {
            return View();
        }
        public JsonResult ProductCodeResult(string code)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrWhiteSpace(code))
            {
                return Json(code, JsonRequestBehavior.AllowGet);
            }

            char[] arr = new char[code.Length];

            int j = 0;
            for (int i = 0; i < code.Length; i++)
            {
                while (char.IsWhiteSpace(code[i]) && char.IsWhiteSpace(code[i + 1]))
                {
                    i++;
                }
                arr[j] = code[i];
                j++;
            }

            return Json((new string(arr)).Replace(" ", ","), JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region download
        public FileResult DownloadJson(string file)
        {
            return DownLoadJsonFile(System.IO.Path.Combine(HostingEnvironment.MapPath(Global.JsonDirectory), file));
        }
        #endregion
    }
}