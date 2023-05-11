using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using SemesterProjekt3Web.ApiAccess;
using SemesterProjekt3Web.Models;
using SemesterProjekt3Web.BusinessLogic;

namespace SemesterProjekt3Web.Controllers
{
    public class BookingController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        string baseURL;
        public BookingController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> CustomerInfo()
        {
            //Vi henter vores seat id'er og putter dem i en liste
            var formData = Request.Form["myListInput"];
            var showingData = Request.Form["showing"];
            Showing realShowing = JsonConvert.DeserializeObject<Showing>(showingData);

            Console.WriteLine("showingdata:" + showingData);
            var myList = new List<string>();
            if (!formData.ToString().Contains(","))
            {
                
                string seat = formData.ToString();
                Console.WriteLine(seat);
                seat = seat.TrimStart('[');
                Console.WriteLine(seat);
                seat = seat.TrimEnd(']');
                Console.WriteLine(seat);
                myList.Add(seat);
            }
            else
            {
                foreach (var item in formData.ToString().Split(','))
                {
                    string addItem = item;
                    if (item.StartsWith("["))
                    {
                        addItem = item.TrimStart('[');
                    }
                    if (item.EndsWith("]"))
                    {
                        addItem = item.TrimEnd(']');
                    }

                    myList.Add(addItem);
                    Console.WriteLine(addItem);
                }

            }
            
            
            Console.WriteLine("Trimmed seat: " + myList);

            //Nu opretter vi vores booking objekt
            Console.WriteLine(realShowing.ShowingId);
            Booking newBooking = new Booking();
            newBooking.BookingId = 0;
            newBooking.Showing = realShowing;
            newBooking.Total = realShowing.MovieCopy.Price * myList.Count;

            SeatsAccess seatAccess = new SeatsAccess();
            List<Seat> seats = new List<Seat>();
            foreach (var seat in myList)
            {
                Seat tempSeat = await seatAccess.GetSeatById(seat);
                Console.WriteLine("Seat: " + tempSeat);
                seats.Add(tempSeat);
            }
            newBooking.BookedSeats = seats;
            return View(newBooking);
        }

        public IActionResult Seats(int id, bool returned = false)
        {
            HttpClient client = new HttpClient();

            var respTask = client.GetAsync($"https://localhost:7155/api/showings/{id}");
            respTask.Wait();
            StreamReader sr = new(respTask.Result.Content.ReadAsStream());
            string stringResult = sr.ReadToEnd();
            sr.Close();
            Showing foundShowing = JsonConvert.DeserializeObject<Showing>(stringResult);
            ViewBag.Returned = returned;
            return View(foundShowing);
        }

        public async Task<IActionResult> Receipt(string name, string email, string phoneNo, string booking)
        {
            Console.WriteLine(booking);
            Booking realBooking = JsonConvert.DeserializeObject<Booking>(booking);
            realBooking.CustomerPhone = phoneNo;
            realBooking.TimeOfPurchase = DateTime.Now;
            ViewBag.CustomerName = name;
            ViewBag.CustomerEmail = email;

            BookingAccessLogic bal = new BookingAccessLogic();
            bool suceeded = await bal.AddBooking(realBooking);
            if (!suceeded)
            {
                
                return RedirectToAction("Seats", "Booking", new {id = realBooking.Showing.ShowingId, returned = true});
            }
            
            return View(realBooking);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
