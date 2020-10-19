using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using OrleansHotelBooking.Interfaces;

namespace OrleansHotelBooking.Grains
{
    public class GuestGrain : Grain, IGuestGrain
    {
        private IPersistentState<Guest> _guest;
        public GuestGrain([PersistentState("guest", "bookingStorage")]
            IPersistentState<Guest> guest)
        {
            _guest = guest;
        }

        public override Task OnDeactivateAsync()
        {
            //_guest.WriteStateAsync();
            return base.OnDeactivateAsync();
        }

        public async Task UpdateInfo(Guest guestInfo)
        {
            _guest.State = guestInfo;
            await _guest.WriteStateAsync();
        }

        public async Task AssignBooking(Guid bookingId)
        {
            if (_guest.State.Bookings == null)
                _guest.State.Bookings = new List<Guid>();
            _guest.State.Bookings.Add(bookingId);
            await _guest.WriteStateAsync();
        }
    }

    //public class HotelJournal : JournaledGrain<>, IHotelGrain
    //{

    //}
}
