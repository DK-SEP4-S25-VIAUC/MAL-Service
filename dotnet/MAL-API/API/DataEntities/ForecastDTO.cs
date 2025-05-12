namespace API.DataEntities;

public class ForecastDTO
{
    public DateTime next_watering_time { get; set; }
    

    public ForecastDTO(double minutes_until_dry, DateTime timeRightNow)
    {
        next_watering_time = CalcNextWateringTime(timeRightNow, minutes_until_dry);
    }

    private DateTime CalcNextWateringTime(DateTime timeRightNow, double minutesUntilDry)
    {
        return timeRightNow.AddMinutes(minutesUntilDry);
    }
}