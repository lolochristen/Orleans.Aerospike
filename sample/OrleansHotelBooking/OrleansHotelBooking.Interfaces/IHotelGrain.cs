using System;
using System.Text;
using System.Threading.Tasks;
using Orleans;

namespace OrleansHotelBooking.Interfaces
{
    public interface IHotelGrain : IGrainWithStringKey
    {
        //Task<Guid> BookRoom(string guestKey, DateTime from, DateTime to, int nbrOfRooms);
        Task<BookingResponse> BookRoom(string guestKey, DateTime from, DateTime to, int nbrOfRooms);

        Task<int> GetAvailableRooms(DateTime date);

        Task<int> GetAvailableRooms(DateTime fromDate, DateTime toDate);

        Task UpdateInfo(Hotel hotelInfo);

        Task SetAvailableRooms(int nbrRooms, DateTime fromDate, DateTime toDate);
    }

    [Serializable]
    public class NoRoomException : Exception
    {
        public string HotelKey { get; set; }
        public DateTime Date { get; set; }
        public int RequestedRooms { get; set; }
    }

    [Serializable]
    public class BookingResponse
    {
        public bool Sucessfull { get; set; }
        public Guid BookingId { get; set; }
    }
}
