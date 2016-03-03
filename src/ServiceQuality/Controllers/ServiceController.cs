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
using System.Text.RegularExpressions;

namespace ServiceQuality.Controllers
{
    public class ServiceController : Controller
    {
        private ApplicationDbContext _context;

        public ServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        [ActionName("History")]
        public IActionResult History()
        {
            return View(_context.Services.ToList());
        }

        // GET: Service
        public IActionResult Index()
        {
            return View();
        }

        class Operation
        {
            public String Method { get; set; }
            public List<Parameter> Parameters { get; set; }
        }

        class Parameter
        {
            public String Name { get; set; }
            public String Type { get; set; }
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

                var elements = schema.Elements(s + "element");

                Func<XElement, string> getName = el => el.Attribute("name").Value;
                Func<XElement, string> getType = el => el.Attribute("type").Value.Replace("s:", "");

                var names = from el in elements
                            let name = getName(el)
                            where el.HasAttributes
                                && !name.EndsWith("Response")
                                && !name.EndsWith("Return")
                                && !name.StartsWith("Array")
                                && char.IsUpper(name[0])
                            select name;

                var operations = new List<Operation>();

                foreach (string n in names)
                {
                    var method = elements.Single(el => getName(el) == n);

                    var parameters = from par in method.Descendants(s + "element")
                                     let name = getName(par)
                                     let type = getType(par)
                                     select new Parameter { Name = name, Type = type };

                    operations.Add(new Operation
                    {
                        Method = n,
                        Parameters = parameters.ToList()
                    });

                }
                return new JsonResult(operations);

            }
            catch (Exception e)
            {
                return new JsonResult("");
            }

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

                if (service == null)
                {
                    return HttpNotFound();
                }

                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                service.Results = service.Results.OrderBy(r => r.Order).ToList();

                return Json(service, jsonSerializerSettings);
            }
            catch (Exception e)
            {
                return Json(null);
            }
        }

        /// <summary>
        /// Lists all the services as JSON
        /// </summary>
        /// <returns></returns>
        [ActionName("Services")]
        public IActionResult Services()
        {
            try
            {
                var services = _context.Services.Include(s => s.Results);

                if (services == null)
                {
                    return HttpNotFound();
                }

                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                return Json(services, jsonSerializerSettings);
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
        public async Task<IActionResult> Create(Service service, String methodSelect)
        {
            if (!service.HasValidType())
            {
                return View(service);
            }

            if (ModelState.IsValid)
            {
                service.Success = false;
                List<string> p = FindParameters();

                service.Url = service.Url.Replace("?", "").Replace("WSDL", "");
                service.Url = service.Url + "/" + methodSelect + "?" + String.Join("&", p);

                _context.Services.Add(service);
                _context.SaveChanges();

                return RedirectToAction("Details", new { id = service.Id });
            }

            return View(service);
        }

        private List<string> FindParameters()
        {
            // http://wsf.cdyne.com/WeatherWS/Weather.asmx/GetCityForecastByZIP?zip=43434

            // all the form's inputs that are part of the methodValue group
            var keys = from k in HttpContext.Request.Form.Keys where k.StartsWith("methodValue") select k;

            var parameters = new List<String>();

            foreach (var k in keys)
            {
                // turn methodValue[abc] to abc
                Match match = Regex.Match(k, @"methodValue\[(.*)\]", RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    var name = match.Groups[1].Value;
                    var val = HttpContext.Request.Form[k];
                    parameters.Add(name + "=" + val);
                }
            }

            return parameters;
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

                            //if (response.IsSuccessStatusCode)
                            //{
                            result.End = DateTime.Now;
                            finishedResults.Add(result);
                            _context.SaveChanges();
                            //}

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
            }
            catch (Exception e)
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
                return RedirectToAction("History");
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
            return RedirectToAction("History");
        }

        // GET: Service/Compare
        public IActionResult Compare()
        {
            return View();
        }
    }
}
