# LANDIS-II support library GitHub URL
$master = "https://github.com/LANDIS-II-Foundation/Support-Library-Dlls-v8/raw/main/"


#************************************************
# LANDIS-II support library dependencies
# Modify here when any dependencies changed

$dlls = "Landis.Library.Metadata-v2.dll",
"Landis.Library.Parameters-v2.dll",
"MathNet.Numerics.dll"
#************************************************

# Tries per library, and seconds to wait between them
$attempts = 3
$wait = 5


# LANDIS-II support libraries download
$current = Get-Location
$outpath = $current.toString() + "/"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

ForEach ($item in $dlls) {
	$dll = $outpath + $item
	$url = $master + $item
	for ($try = 1; $try -le $attempts; $try++) {
		try {
			Invoke-WebRequest -uri $url -Outfile $dll -ErrorAction Stop
			$item + "------------- downloaded"
			break
		}
		catch {
			"Try " + $try + " of " + $attempts + " failed for " + $url + ": " + $_.Exception.Message
			if ($try -eq $attempts) {
				# Stop with an error, so that a build does not go on without this library
				"Unable to download file from " + $url
				exit 1
			}
			Start-Sleep -Seconds $wait
		}
	}
}
"`n***** Download complete *****`n"

