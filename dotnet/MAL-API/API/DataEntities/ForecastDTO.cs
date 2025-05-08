namespace API.DataEntities;

public class ForecastDTO
{
    public DateTime next_watering_time { get; set; }
    

    public ForecastDTO(double minutes_until_dry, DateTime lastSampleTimeStamp)
    {
        next_watering_time = CalcNextWateringTime(lastSampleTimeStamp, minutes_until_dry);
    }

    private DateTime CalcNextWateringTime(DateTime lastSampleTimeStamp, double minutesUntilDry)
    {
        return lastSampleTimeStamp.AddMinutes(minutesUntilDry);
    }
}