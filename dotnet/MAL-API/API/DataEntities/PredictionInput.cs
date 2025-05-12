namespace API.DataEntities
{
    public class PredictionInput
    {
        public double? SoilHumidity { get; set; }
        public double SoilDelta { get; set; }
        public double? AirHumidity { get; set; }
        public double? Temperature { get; set; }
        public double? Light { get; set; }
        public double HourSin { get; set; }
        public double HourCos { get; set; }
        public double? Threshold { get; set; }
        public DateTime Timestamp { get; set; }

        public static PredictionInput FromValues(
            double? soilHumidity,
            double? previousSoilHumidity,
            double? airHumidity,
            double? temperature,
            double? light,
            DateTime timestamp,
            double? threshold)
        {
            double soilDelta = soilHumidity ?? 0.0 - previousSoilHumidity ?? 0.0;

            double angle = timestamp.Hour / 24.0 * 2 * Math.PI;
            double hourSin = Math.Sin(angle);
            double hourCos = Math.Cos(angle);

            return new PredictionInput
            {
                SoilHumidity = soilHumidity,
                SoilDelta = soilDelta,
                AirHumidity = airHumidity,
                Temperature = temperature,
                Light = light,
                HourSin = hourSin,
                HourCos = hourCos,
                Threshold = threshold,
                Timestamp = timestamp
            };
        }
    }
}