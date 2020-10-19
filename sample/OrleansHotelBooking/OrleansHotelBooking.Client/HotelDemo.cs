using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Runtime;
using OrleansHotelBooking.Interfaces;

namespace OrleansHotelBooking.Client
{
    public class HotelDemo : IHostedService
    {
        private const int TotalHotels = 400;
        private const int TotalGuests = 5000;
        private const int TotalBookings = 40000;

        private readonly IClusterClient _client;

        private int _sequenceNbr = 0;

        private  Random _random = new Random((int) DateTime.Now.Ticks);

        private int _bookingsSuccessful = 0, _bookingsNoRoom = 0, _bookingsFailed = 0;

        public HotelDemo(IClusterClient client)
        {
            this._client = client;
        }

        private int GetNextSequence()
        {
            return Interlocked.Increment(ref _sequenceNbr);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Test Scenario: {TotalHotels} Hotels, {TotalGuests} Guest do {TotalBookings} Bookings.");
            Console.WriteLine("[ENTER] to start...");
            Console.ReadLine();

            Console.WriteLine("Create Hotels and Guests...");

            var hotelTasks = new List<Task<string>>();
            var guestTasks = new List<Task<string>>();

            for (int i = 0; i < TotalHotels; i++)
            {
                hotelTasks.Add(CreateHotel());
            }

            for (int i = 0; i < TotalGuests; i++)
            {
                guestTasks.Add(CreateGuest());
            }

            Task.WaitAll(hotelTasks.ToArray());
            var hotels = hotelTasks.Select(p => p.Result).ToArray();

            Task.WaitAll(guestTasks.ToArray());
            var guests = guestTasks.Select(p => p.Result).ToArray();

            var start = DateTime.Now;
            var bookingTasks = new List<Task<BookingResponse>>();

            Console.WriteLine("\nCreate Bookings 1/2");
            for (int i = 0; i < TotalBookings / 2; i++)
            {
                var hotelKey = hotels[_random.Next(0, hotels.Length - 1)];
                var guestKey = guests[_random.Next(0, guests.Length - 1)];
                bookingTasks.Add(Book(hotelKey, guestKey));
            }

            Task.WaitAll(bookingTasks.ToArray());

            //Console.WriteLine("\nEnforce deactivate all");
            //IManagementGrain managementGrain = _client.GetGrain<IManagementGrain>(0);
            //await managementGrain.ForceActivationCollection(new TimeSpan(0, 0, 0));

            Console.WriteLine("\nCreate Bookings 2/2");
            bookingTasks.Clear();
            for (int i = 0; i < TotalBookings / 2; i++)
            {
                var hotelKey = hotels[_random.Next(0, hotels.Length - 1)];
                var guestKey = guests[_random.Next(0, guests.Length - 1)];
                bookingTasks.Add(Book(hotelKey, guestKey));
            }

            Task.WaitAll(bookingTasks.ToArray());

            Console.WriteLine($"\nDone in {(DateTime.Now - start)}: Successful:{_bookingsSuccessful} NoRoom:{_bookingsNoRoom} Failed:{_bookingsFailed}  ");
        }
 
        async Task<string> CreateHotel()
        {
            var name = Faker.Company.Name();
            var id = "HOTEL" + GetNextSequence(); // + "_" + name.ToUpper().Replace(' ', '_');
            var hotelAGrain = _client.GetGrain<IHotelGrain>(id);
            var hotelAInfo = new Hotel() { HotelName = "Hotel " + name, AvailableRooms = new Dictionary<DateTime, int>() };
            await hotelAGrain.UpdateInfo(hotelAInfo);
            await hotelAGrain.SetAvailableRooms(20, new DateTime(2020, 6, 1), new DateTime(2020, 12, 31));
            return id;
        }

        async Task<string> CreateGuest()
        {
            var name = Faker.Name.FullName();
            var id = "GUEST" + GetNextSequence();
            var guest1Grain = _client.GetGrain<IGuestGrain>(id);
            await guest1Grain.UpdateInfo(new Guest() { Name = name, Email = Faker.Internet.Email(name) });
            return id;
        }

        async Task<BookingResponse> Book(string hotelKey, string guestKey)
        {
            Console.Write(".");
            //Console.WriteLine($"Book {guestKey} {hotelKey}");
            var hotelAGrain = _client.GetGrain<IHotelGrain>(hotelKey);
            var guest1Grain = _client.GetGrain<IGuestGrain>(guestKey);

            DateTime fromDate = new DateTime(2020, 6, 1).AddDays(_random.Next(0, 180));
            DateTime toDate = fromDate.AddDays(_random.Next(1, 20));

            var requestRooms = _random.Next(1, 4);
            BookingResponse response = null;
            try
            {
                response = await hotelAGrain.BookRoom(guest1Grain.GetPrimaryKeyString(), fromDate, toDate, requestRooms);
                if (response.Sucessfull)
                {
                    Console.Write("+");
                    Interlocked.Increment(ref _bookingsSuccessful);
                }
                else
                {
                    Console.Write("@");
                    Interlocked.Increment(ref _bookingsNoRoom);
                }
            }
            catch (Exception exp)
            {
                Console.Write("E["+exp.Message+"]");
                Interlocked.Increment(ref _bookingsFailed);
            }
            return response;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
