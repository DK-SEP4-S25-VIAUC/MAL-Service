namespace API.DataEntities;

public class SampleDTO
{
    public DateTime Timestamp { get; set; }
    public double? Soil_Humidity { get; set; }
    public double? Air_Humidity { get; set; }
    public double? Air_Temperature { get; set; }
    public double? Light_Value { get; set; }
    public double? Lower_Threshold { get; set; }
    
}
