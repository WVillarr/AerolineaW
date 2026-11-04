param([string]$BaseUrl = 'http://localhost:8080')

$ErrorActionPreference = 'Stop'
$api = $BaseUrl.TrimEnd('/') + '/api/v1'

function Invoke-Api {
    param([string]$Method, [string]$Path, $Body)
    $parameters = @{ Method = $Method; Uri = "$api/$Path" }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = $Body | ConvertTo-Json -Depth 5
    }
    $result = Invoke-RestMethod @parameters
    return $result
}

$airports = @(Invoke-Api GET 'airports')
if ($airports.Count -lt 2) {
    throw 'Start the API in Development with its demo seed: at least two airports are required.'
}

$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 12).ToUpperInvariant()
$aircraft = Invoke-Api POST 'aircraft' @{
    registration = "DEMO$suffix"; model = 'Portfolio demo'; seatCapacity = 6; maxPayloadKg = 1000
}
$passenger = Invoke-Api POST 'passengers' @{
    fullName = 'Portfolio Demo'; documentType = 'Passport'; documentNumber = "DEMO$suffix"
    country = 'Dominican Republic'; phone = '+1 809 555 0100'
}
$departure = [DateTime]::UtcNow.AddHours(12)
$flight = Invoke-Api POST 'flights' @{
    flightNumber = 'DM' + $suffix.Substring(0, 8); aircraftId = $aircraft.id
    originAirportId = $airports[0].id; destinationAirportId = $airports[1].id
    scheduledDepartureUtc = $departure.ToString('o'); scheduledArrivalUtc = $departure.AddHours(2).ToString('o')
}
$request = @{ passengerId = $passenger.id; seat = '1A'; baggageWeightKg = 10; price = 100 }
$booking = Invoke-Api POST "flights/$($flight.id)/bookings" $request
$lookup = Invoke-Api GET "bookings/by-reference/$($booking.reference)"
$checkedIn = Invoke-Api POST "bookings/$($booking.id)/check-in"
$cancelled = Invoke-Api POST "bookings/$($booking.id)/cancel"
$replacement = Invoke-Api POST "flights/$($flight.id)/bookings" $request
$after = Invoke-Api GET "flights/$($flight.id)"
$history = @(Invoke-Api GET "flights/$($flight.id)/bookings")

if ($lookup.id -ne $booking.id -or $checkedIn.status -ne 'CheckedIn' -or
    $cancelled.status -ne 'Cancelled' -or $replacement.status -ne 'Confirmed' -or
    $replacement.id -eq $booking.id -or $after.seatsBooked -ne 1 -or $history.Count -ne 2) {
    throw 'The booking lifecycle did not preserve the expected history and capacity.'
}

[PSCustomObject]@{
    FlightId = $flight.id; OriginalBookingId = $booking.id; OriginalReference = $booking.reference
    ReplacementBookingId = $replacement.id; ReplacementReference = $replacement.reference
    Seat = $replacement.seat; SeatsBooked = $after.seatsBooked; HistoryCount = $history.Count
    Result = 'Passed'
}
