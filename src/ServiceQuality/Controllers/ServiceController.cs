using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Data.Entity;
using ServiceQuality.Models;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace ServiceQuality.Controllers
{
    public class ServiceController : Controller
    {
        private ServiceDbContext _context;

        public ServiceController(ServiceDbContext context)
        {
            _context = context;
        }

        // GET: Service
        public IActionResult Index()
        {
            return View(_context.Services.ToList());
        }

        // GET: Service/Details/5
        public IActionResult Details(int? id)
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

        // GET: Service/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Service/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service, string type)
        {
            if (type != null && (type.Equals("Capacity") || type.Equals("Distribution")))
            {
                service.Type = type;
            }

            if (ModelState.IsValid)
            {
                service.Success = false;

                _context.Services.Add(service);
                _context.SaveChanges();

                MakeRequests(service);

                return RedirectToAction("Index");
            }

            return View(service);
        }

        async private void MakeRequests(Service service)
        {
            var _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");

            int[] intArray = Enumerable.Range(0, service.Requests).ToArray();

            int finished = 0;

            var result = new Result();

            result.Start = new DateTime();

            _context.Results.Add(result);
            service.Results.Add(result);

            var results = intArray
                .Select(async t =>
                {

                    using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, service.Url))
                    {

                        try
                        {

                            var response = await _httpClient.SendAsync(requestMessage);
                            var responseContent = await response.Content.ReadAsStringAsync();

                            // check that it succeeded
                            finished += 1;

                            if (finished == service.Requests)
                            {
                                service.Success = true;
                                result.End = new DateTime();
                                _context.SaveChanges();
                                _context.Dispose();
                            }

                            if (t == service.Requests)
                            {
                                result.End = new DateTime();
                                result.SucessfullRequests = finished;
                                _context.SaveChanges();
                                _context.Dispose();
                            }

                            return responseContent;
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                });

            Task.WaitAll(results.ToArray());
        }

        async private void MakeRequestsOld(Service service)
        {
            // skips execution of rough code below
            //return;

            // TODO: Make web service requests
            // Create a new thread to run the requests

            int finishedRequests = 0;

            var client = new HttpClient();
            client.Timeout = new System.TimeSpan(30000); // 30 secs
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");


            // create a loop here
            // for (int i = 0; i < service.Requests; i++)

            var results = new Result();
            results.Start = new System.DateTime();

            using (HttpResponseMessage response = await client.GetAsync(service.Url))
            {

                if (response.StatusCode == HttpStatusCode.OK) // 200 status code
                {
                    finishedRequests++;

                    // When the requests are the same as the required ones save to the db?
                    if (finishedRequests == service.Requests)
                    {
                        //using (var db = new ServiceDbContext(null))
                        //{
                        //    service.Success = true;
                        //    results.End = new System.DateTime();

                        //    if (service.Type.Equals("Capacity"))
                        //    {
                        //        results.SucessfullRequests = finishedRequests;
                        //    }

                        //    service.Results.Add(results);

                        //    db.SaveChanges();
                        //}
                    }
                }
            }

            client.Dispose();
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
            Service service = _context.Services.Single(m => m.Id == id);
            _context.Services.Remove(service);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
