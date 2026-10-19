import { useContext } from 'react'
import { WeatherForecastContext } from './WeatherForecastContext'

export function useWeatherForecast() {
  const context = useContext(WeatherForecastContext)

  if (!context) {
    throw new Error(
      'useWeatherForecast must be used within a WeatherForecastProvider',
    )
  }

  return context
}
