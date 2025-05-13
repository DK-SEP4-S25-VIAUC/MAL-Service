namespace API.DataEntities;

public class SampleDTO
{
    public DateTime timestamp { get; set; }
    public double? soil_humidity { get; set; }
    public double? air_humidity { get; set; }
    public double? air_temperature { get; set; }
    public double? light_value { get; set; }
    public double? lower_threshold { get; set; }
    
}
