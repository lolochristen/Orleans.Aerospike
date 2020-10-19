using System.Threading.Tasks;
using Orleans;

namespace OrleansHotelBooking.Interfaces
{
    public interface IBookingGrain : IGrainWithGuidKey
    {
        Task Initialize(Booking booking);

        Task CancelBooking();
    }
}
