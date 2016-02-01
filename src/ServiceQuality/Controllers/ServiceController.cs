using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Data.Entity;
using ServiceQuality.Models;
using System.Threading;
using System.Net;
using System.Net.Http;

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
        public IActionResult Create(Service service, string type)
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

        private void MakeRequests(Service service)
        {
            // skips execution of rough code below
            return;

            // TODO: Make web service requests
            // Create a new thread to run the requests

            new Thread(async () =>
            {
                int finishedRequests = 0;

                using (var client = new HttpClient())
                {
                    client.Timeout = new System.TimeSpan(30000); // 30 secs

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
                                using (var db = new ApplicationDbContext())
                                {
                                    service.Success = true;
                                    results.End = new System.DateTime();

                                    if (service.Type.Equals("Capacity"))
                                    {
                                        results.SucessfullRequests = finishedRequests;
                                    }

                                    service.Results.Add(results);

                                    db.SaveChanges();
                                }
                            }
                        }
                    }
                }
            });
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
