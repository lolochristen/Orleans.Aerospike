using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.EventSourcing;
using Orleans.EventSourcing.CustomStorage;
using Orleans.Runtime;
using OrleansHotelBooking.Interfaces;

namespace OrleansHotelBooking.Grains
{
    public class HotelGrain : Grain, IHotelGrain
    {
        private readonly IPersistentState<Hotel> _hotel;

        public HotelGrain([PersistentState("hotel", "bookingStorage")] IPersistentState<Hotel> hotel)
        {
            _hotel = hotel;
        }

        public override Task OnDeactivateAsync()
        {
            return base.OnDeactivateAsync();
        }

        public async Task<BookingResponse> BookRoom(string guestKey, DateTime fromDate, DateTime toDate, int nbrOfRooms)
        {
            var guest = GrainFactory.GetGrain<IGuestGrain>(guestKey);

            // check availability
            var availableRooms = await GetAvailableRooms(fromDate, toDate);

            if (availableRooms <= nbrOfRooms)
                return new BookingResponse() { Sucessfull = false };
                //throw new NoRoomException() { HotelKey = this.GetPrimaryKeyString(), Date = toDate, RequestedRooms = nbrOfRooms }; 

            for (int d = 0; d < (toDate - fromDate).Days; d++)
            {
                DateTime date = fromDate.AddDays(d);
                _hotel.State.AvailableRooms[date] -= nbrOfRooms;
            }

            var bookingId = Guid.NewGuid();
            var bookingGrain = GrainFactory.GetGrain<IBookingGrain>(bookingId);

            var booking = new Booking()
            {
                HotelKey = this.GetPrimaryKeyString(),
                GuestKey = guestKey,
                From = fromDate,
                To = toDate,
                NbrRooms = nbrOfRooms
            };

            await bookingGrain.Initialize(booking); 

            await guest.AssignBooking(bookingId);

            if (_hotel.State.Bookings == null)
                _hotel.State.Bookings = new List<Guid>();
            _hotel.State.Bookings.Add(bookingId);

            await _hotel.WriteStateAsync();

            return new BookingResponse() { BookingId = bookingId, Sucessfull = true };
        }

        public Task<int> GetAvailableRooms(DateTime date)
        {
            return GetAvailableRooms(date, date);
        }

        public  Task<int> GetAvailableRooms(DateTime fromDate, DateTime toDate)
        {
            try
            {
                return Task.FromResult(_hotel.State.AvailableRooms.Where(d => d.Key >= fromDate && d.Key <= toDate)
                    .Min(d => d.Value));
            }
            catch (Exception e)
            {
                return Task.FromResult(0);
            }
        }

        public async Task UpdateInfo(Hotel hotelInfo)
        {
            _hotel.State = hotelInfo;
            await _hotel.WriteStateAsync();
        }

        public async Task SetAvailableRooms(int nbrRooms, DateTime fromDate, DateTime toDate)
        {
            if (_hotel.State.AvailableRooms == null)
                _hotel.State.AvailableRooms = new Dictionary<DateTime, int>();

            for (int d = 0; d < (toDate - fromDate).Days; d++)
            {
                DateTime date = fromDate.AddDays(d);
                if (_hotel.State.AvailableRooms.ContainsKey(date))
                    _hotel.State.AvailableRooms[date] = nbrRooms;
                else
                    _hotel.State.AvailableRooms.Add(date, nbrRooms);
            }

            await _hotel.WriteStateAsync();
        }
    }
}
