namespace API.DataEntities;

public class ForecastDTO
{
    public int next_watering_time { get; set; }

    public ForecastDTO(int nextWateringTime)
    {
        next_watering_time = nextWateringTime;
    }
}