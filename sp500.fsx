#r "nuget: CsvHelper, 30.0.1"

open System.IO
open System.Net.Http
open CsvHelper

type SP500RecordRaw = {
    DATE  : string
    SP500 : string
}

type SP500Record = {
    date  : string
    value : float
}

let get_sp500 date =
    let config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)

    config.PrepareHeaderForMatch <- fun args -> args.Header.ToLower()

    let uri = sprintf  "https://fred.stlouisfed.org/graph/fredgraph.csv?id=%s&cosd=%s" "SP500" "2023-01-01"
            
    let str = (new HttpClient()).GetStringAsync(uri).Result

    // let records = (new CsvReader(new StringReader(str), config)).GetRecords<SP500Record>()

    // let arr = records |> Array.ofSeq

    let arr = (new CsvReader(new StringReader(str), config)).GetRecords<SP500RecordRaw>() |> Array.ofSeq |> Array.filter (fun elt -> elt.SP500 <> ".") |> Array.map (fun elt -> { date = elt.DATE; value = float elt.SP500 })

    arr



let date = "2023-01-01"

let arr = get_sp500 date

for elt in arr do
    printfn "%10s %15f" elt.date elt.value


// for elt in arr do
//     printfn "%10s %15s" elt.DATE elt.SP500

// let result = arr |> Array.filter (fun elt -> elt.SP500 <> ".") |> Array.map (fun elt -> { date = elt.DATE; value = float elt.SP500 })

exit 0

// let result_alt = arr |> Array.where (fun elt -> elt.SP500 = ".")

// result_alt |> format_table