using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using OrleansHotelBooking.Interfaces;

namespace OrleansHotelBooking.Grains
{
    public class BookingGrain : Grain, IBookingGrain
    {
        private IPersistentState<Booking> _booking;
        public BookingGrain([PersistentState("booking", "bookingStorage")]
            IPersistentState<Booking> booking)
        {
            _booking = booking;
        }

        public override Task OnDeactivateAsync()
        {
            //_booking.WriteStateAsync();
            return base.OnDeactivateAsync();
        }

        public async Task Initialize(Booking booking)
        {
            _booking.State = booking;
            await _booking.WriteStateAsync();
        }

        public async Task CancelBooking()
        {
        }
    }
}
