
USE ROLE "ACCOUNTADMIN";
-- Give role permission to create account integration
GRANT CREATE INTEGRATION ON ACCOUNT TO ROLE DOD_DEVELOPER;

USE ROLE "DOD_DEVELOPER";
USE DATABASE "DOD2026";
USE WAREHOUSE "DOD_WAREHOUSE";
USE SCHEMA "PUBLIC";

CREATE OR REPLACE NETWORK RULE OPEN_METEO_NETWORK_RULE 
    MODE = EGRESS 
    TYPE = HOST_PORT 
    VALUE_LIST = ( 'geocoding-api.open-meteo.com', 'api.open-meteo.com' );

CREATE OR REPLACE EXTERNAL ACCESS INTEGRATION OPEN_METEO_INTEGRATION 
    ALLOWED_NETWORK_RULES = (OPEN_METEO_NETWORK_RULE) 
    ENABLED = TRUE;

CREATE OR REPLACE PROCEDURE PRC_GET_WEATHER(CITY VARCHAR)
    RETURNS TABLE("City" VARCHAR, "Country" VARCHAR, "Latitude" FLOAT, "Longitude" FLOAT, "Temperature" FLOAT, "WeatherCode" INT, "WindSpeed" FLOAT, "Time" VARCHAR)
    LANGUAGE PYTHON
    RUNTIME_VERSION = '3.11'
    HANDLER = 'main'
    EXTERNAL_ACCESS_INTEGRATIONS = (OPEN_METEO_INTEGRATION)
    PACKAGES = ('requests', 'snowflake-snowpark-python')
AS
$$
import requests

def main(session, city):

    # Step 1: Convert city name to latitude/longitude
    geocoding_url = "https://geocoding-api.open-meteo.com/v1/search"

    geocoding_params = {
        "name": city,
        "count": 1,
        "language": "en",
        "format": "json"
    }

    response = requests.get(
        geocoding_url,
        params=geocoding_params,
        timeout=30
    )

    response.raise_for_status()

    location_data = response.json()

    if "results" not in location_data or not location_data["results"]:
        return session.create_dataframe(
            [[city, None, None, None, None, None, None, None]],
            schema=["CITY", "COUNTRY", "LATITUDE", "LONGITUDE", "TEMPERATURE_C", "WEATHER_CODE", "WIND_SPEED_KMH", "WEATHER_TIME"]
        )

    location = location_data["results"][0]

    latitude = location["latitude"]
    longitude = location["longitude"]
    city_name = location["name"]
    country = location.get("country", "")

    # Step 2: Get current weather
    weather_url = "https://api.open-meteo.com/v1/forecast"

    weather_params = {
        "latitude": latitude,
        "longitude": longitude,
        "current": "temperature_2m,weather_code,wind_speed_10m"
    }

    response = requests.get(
        weather_url,
        params=weather_params,
        timeout=30
    )

    response.raise_for_status()

    weather = response.json()["current"]

    # Step 3: Return the result as a table row
    return session.create_dataframe(
        [[city_name, country, latitude, longitude, weather["temperature_2m"], weather["weather_code"], weather["wind_speed_10m"], weather["time"]]],
        schema=["City", "Country", "Latitude", "Longitude", "Temperature", "WeatherCode", "WindSpeed", "Time"]
    )
$$;



CALL PRC_GET_WEATHER('Vienna');

CALL PRC_GET_WEATHER('London');

CALL PRC_GET_WEATHER('Rio de Janeiro');
