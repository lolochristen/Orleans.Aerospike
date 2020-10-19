using System;

namespace OrleansHotelBooking.Interfaces
{
    public class Booking
    {
        public string HotelKey { get; set; }
        public string GuestKey { get; set; }
        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public int NbrRooms { get; set; }
    }
}
