#r "nuget: CsvHelper, 30.0.1"

open System.IO
open System.Net.Http
open CsvHelper

// type WALCLRecord() =
//     member val DATE = "" with get, set
//     member val WALCL = "" with get, set

// type WALCLRecord = {
//     DATE : string
//     WALCL : float
// }

type WALCLRecord = {
    [<CsvHelper.Configuration.Attributes.NameAttribute("DATE")>]
    date : string
    [<CsvHelper.Configuration.Attributes.NameAttribute("WALCL")>]
    walcl : float
}

// type WALCLRecord = {
//     date : string
//     walcl : float
// }

// let config = CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)

// config.PrepareHeaderForMatch = fun args -> args.Header.ToLower()

// config.PrepareHeaderForMatch <- fun (header : string) -> header.ToLower()

// config.PrepareHeaderForMatch <- fun header -> header

// config.PrepareHeaderForMatch <- fun args -> args.Header.ToLower()

let config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)

config.PrepareHeaderForMatch <- fun args -> args.Header.ToLower()



let uri = sprintf  "https://fred.stlouisfed.org/graph/fredgraph.csv?id=%s&cosd=%s" "WALCL" "2023-01-01"
        
let str = (new HttpClient()).GetStringAsync(uri).Result

use string_reader = new StringReader(str)

// use csv_reader = new CsvReader(string_reader, System.Globalization.CultureInfo.InvariantCulture)

use csv_reader = new CsvReader(string_reader, config)

// csv_reader.Configuration.PrepareHeaderForMatch <- fun header -> header.ToLower()

let records = csv_reader.GetRecords<WALCLRecord>()

let arr = records |> Array.ofSeq

// for elt in arr do
//     printfn "%10s %15s" elt.DATE elt.WALCL

for elt in arr do
    printfn "%10s %15f" elt.date elt.walcl

exit 0