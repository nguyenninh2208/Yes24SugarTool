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
    public class HomeController : Controller
    {
        private const string DataTableCacheName = "DataExcelCache";
        public HomeController()
        {

        }
        public ActionResult Index()
        {
            return Content("Please comeback later.");
        }
        public ViewResult ExportJson()
        {
            if(Request["clearcache"] != null)
            {

                System.Web.Caching.Cache _Cache = HttpContext.Cache;
                _Cache.Remove(DataTableCacheName);
            }
            return View();
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult ExportJson(HttpPostedFileBase file)
        {
            try
            {
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
        public ActionResult ProcessData(FormCollection collection)
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

                    TempData["Json"] = JsonRepos.JsonPrettify(JsonRepos.ConvertFromDataTable(dt));

                    //  System.IO.File.WriteAllText(System.IO.Path.Combine(HostingEnvironment.MapPath(Global.JsonDirectory), "Output.json"), json);


                }

            }

            return RedirectToAction("ExportJson");
        }
    }
}