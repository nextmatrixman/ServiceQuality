using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Data.Entity;
using ServiceQuality.Models;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using System.Xml;
using System.Xml.Linq;

namespace ServiceQuality.Controllers
{
    public class ServiceController : Controller
    {
        private ApplicationDbContext _context;

        public ServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Service
        public IActionResult Index()
        {
            return View(_context.Services.ToList());
        }

        class Operation
        {
            public String Type { get; set; }
            public String Method { get; set; }
            public String Parameter { get; set; }
        }

        [ActionName("GetServiceDescription")]
        public JsonResult GetServiceDescription(String url)
        {
            var urls = new List<string>();

            try
            {

            var x = XDocument.Load(url);
            XNamespace wsdl = "http://schemas.xmlsoap.org/wsdl/";
            XNamespace s = "http://www.w3.org/2001/XMLSchema";

            var schema = x.Root
                .Element(wsdl + "types")
                .Element(s + "schema");

            var elements = schema.Elements(s + "element"); ;

            Func<XElement, string> getName = el => el.Attribute("name").Value;
            Func<XElement, string> getType = el => el.Attribute("type").Value.Replace("s:", "");

            var names = from el in elements
                        let name = getName(el)
                        where el.HasAttributes
                            && !name.EndsWith("Response")
                            && !name.EndsWith("Return")
                            && !name.StartsWith("Array")
                            && name.Contains("Get")
                        select name;

            var d = new List<Operation>();

            foreach (string n in names)
            {
                var method = elements.Single(el => getName(el) == n);

                var parameters = from par in method.Descendants(s + "element")
                                 let name = getName(par)
                                 let type = getType(par)
                                 select new Operation { Method = n, Parameter = name, Type = type };

                if (parameters.Count() == 0)
                {
                    d.Add(new Operation
                    {
                        Method = n
                    });
                }

                foreach (Operation o in parameters)
                {
                    d.Add(o);
                }

            }
                return new JsonResult(d);

            }
            catch (Exception e)
            {

            }

            return new JsonResult("");

        }

        // GET: Service/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            Service service = _context.Services.Include(s => s.Results).Single(m => m.Id == id);
            service.Results = service.Results.OrderBy(r => r.Order).ToList();

            if (service == null)
            {
                return HttpNotFound();
            }

            return View(service);
        }

        [ActionName("Json")]
        public IActionResult Json(int id)
        {
            try
            {
                Service service = _context.Services.Include(s => s.Results).Single(m => m.Id == id);
                service.Results = service.Results.OrderBy(r => r.Order).ToList();

                if (service == null)
                {
                    return HttpNotFound();
                }
                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                return Json(service, jsonSerializerSettings);
            }
            catch (Exception e)
            {
                return Json(null);
            }

        }

        // GET: Service/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Service/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service, String methodName, String methodParam, String methodValue)
        {
            if (!service.HasValidType())
            {
                return View(service);
            }

            if (ModelState.IsValid)
            {
                service.Success = false;

                // http://wsf.cdyne.com/WeatherWS/Weather.asmx/GetCityForecastByZIP?zip=43434

                service.Url = service.Url.Replace("?", "").Replace("WSDL", "");
                service.Url = service.Url + "/" + methodName + "?" + methodParam + "=" + methodValue;

                _context.Services.Add(service);
                _context.SaveChanges();

                return RedirectToAction("Details", new { id = service.Id });
            }

            return View(service);
        }

        [ActionName("RunTest")]
        public void RunTest(int id)
        {
            Service service = _context.Services.Include(s => s.Results).Single(m => m.Id == id);
            service.Results = service.Results.OrderBy(r => r.Order).ToList();

            if (service == null || service.Results.Count > 0)
            {
                return;
            }

            if (service.Type.Equals("Capacity"))
            {
                MakeCapacityRequests(service);
            }
            else if (service.Type.Equals("Distribution"))
            {
                MakeDistributionRequests(service);
            }
        }

        private void MakeCapacityRequests(Service service)
        {
            for (int i = 0; i < service.Requests; i += 1)
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");

                using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, service.Url))
                {
                    try
                    {
                        var result = new Result();
                        result.Service = service;
                        result.Start = DateTime.Now;
                        result.Order = i + 1;

                        _context.Results.Add(result);
                        _context.SaveChanges();

                        // WORKAROUND TO EXCEPTION ISSUE
                        //Thread.Sleep(500);

                        var response = client.SendAsync(requestMessage).Result;
                        //var responseContent = await response.Content.ReadAsStringAsync();

                        //if (response.IsSuccessStatusCode)
                        //{
                            if (i == service.Requests - 1)
                            {
                                service.Success = true;
                            }

                            result.End = DateTime.Now;
                            _context.SaveChanges();
                        //}

                    }
                    catch (Exception ex)
                    {
                        //throw ex;
                    }
                }

                client.Dispose();
            }
        }

        // Todo: Distribution needs a few more changes
        private void MakeDistributionRequests(Service service)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");

            var finishedResults = new List<Result>();

            int[] intArray = Enumerable.Range(0, service.Requests).ToArray();

            var results = intArray
                .Select(t =>
                {
                    using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, service.Url))
                    {
                        try
                        {
                            var result = new Result();
                            result.Service = service;
                            result.Start = DateTime.Now;
                            result.Order = t;

                            _context.Results.Add(result);
                            _context.SaveChanges();

                            var response = client.SendAsync(requestMessage).Result;
                            //var responseContent = response.Content.ReadAsStringAsync().Result;

                            if (response.IsSuccessStatusCode)
                            {
                                result.End = DateTime.Now;
                                finishedResults.Add(result);
                                _context.SaveChanges();
                            }

                            return Task.FromResult(result.Id);
                        }
                        catch (Exception ex)
                        {
                            //throw ex;
                            return Task.FromResult(0);
                        }
                    }
                });

            try
            {
                Task.WaitAll(results.ToArray());
            } catch (Exception e)
            {
                //View(e);
            }

            //Thread.Sleep(5000);

            service.Success = finishedResults.Count == service.Requests;
            _context.SaveChanges();
            //_context.Dispose();
        }

        // GET: Service/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            Service service = _context.Services.Single(m => m.Id == id);
            if (service == null)
            {
                return HttpNotFound();
            }
            return View(service);
        }

        // POST: Service/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Service service)
        {
            if (ModelState.IsValid)
            {
                _context.Update(service);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(service);
        }

        // GET: Service/Delete/5
        [ActionName("Delete")]
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            Service service = _context.Services.Single(m => m.Id == id);
            if (service == null)
            {
                return HttpNotFound();
            }

            return View(service);
        }

        // POST: Service/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            Service service = _context.Services.Include(s => s.Results).Single(m => m.Id == id);

            var r = service.Results.ToList();
            r.ForEach(x => _context.Results.Remove(x));

            _context.Services.Remove(service);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
