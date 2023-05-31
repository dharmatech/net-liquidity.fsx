#r "nuget: CsvHelper, 30.0.1"

open System.IO
open System.Net.Http
open CsvHelper

type WALCLRecord = {
    DATE : string
    WALCL : float
}

let config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)

config.PrepareHeaderForMatch <- fun args -> args.Header.ToLower()

let uri = sprintf  "https://fred.stlouisfed.org/graph/fredgraph.csv?id=%s&cosd=%s" "WALCL" "2023-01-01"
        
let str = (new HttpClient()).GetStringAsync(uri).Result

use string_reader = new StringReader(str)

use csv_reader = new CsvReader(string_reader, config)

let records = csv_reader.GetRecords<WALCLRecord>()

let arr = records |> Array.ofSeq

for elt in arr do
    printfn "%10s %15f" elt.DATE elt.WALCL

exit 0