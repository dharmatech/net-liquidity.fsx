#r "nuget: CsvHelper, 30.0.1"

open System
open System.IO
open System.Net.Http
open System.Net.Http.Json
open System.Text.Json

open CsvHelper

fsi.ShowDeclarationValues <- false
// ----------------------------------------------------------------------
type FREDRecordRaw = {
    date : string
    value : string
}

type FREDRecord = {
    date : string
    value : decimal
}
// ----------------------------------------------------------------------
let download_fred_series series date =

    printfn "Retrieving records since: %s" date

    let uri = sprintf  "https://fred.stlouisfed.org/graph/fredgraph.csv?id=%s&cosd=%s" series date
        
    let str = (new HttpClient()).GetStringAsync(uri).Result
        
    let new_str = String.Join(Environment.NewLine, 
        Array.append [| "date,value" |]  (str.Split(Environment.NewLine.ToCharArray()) |> Array.skip 1))

    let config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        
    config.PrepareHeaderForMatch <- fun args -> args.Header.ToLower()    

    let data = (new CsvReader(new StringReader(new_str), config)).GetRecords<FREDRecordRaw>() |> Array.ofSeq

    printfn "Retrieved %d records" data.Length

    data

let get_fred_series_raw series date =
    let path = sprintf "%s.json" series
        
    if File.Exists(path) then         

        let data = JsonSerializer.Deserialize<FREDRecordRaw[]>(File.ReadAllText(path))        
        let last_date = data[data.Length-1].date
        let result_alt = download_fred_series series last_date

        let to_add = result_alt |> Array.where (fun elt -> elt.date > last_date)

        printfn "Adding %d records" to_add.Length
                
        if to_add.Length > 0 then
            let new_data = Array.append data to_add
            File.WriteAllText(path, JsonSerializer.Serialize new_data)
            new_data
        else
            data        
    else
        let data = download_fred_series series date
        File.WriteAllText(path, JsonSerializer.Serialize data)
        data

let get_fred_series series date =
    get_fred_series_raw series date |>
    Array.filter (fun elt -> elt.value <> ".") |> 
    Array.map (fun elt -> { date = elt.date; value = decimal elt.value })    

// let result = get_fred_series_raw "RRPONTSYD" "2023-03-01"
let result = get_fred_series "RRPONTSYD" "2023-03-01"

// let result = get_fred_series "WALCL" "2023-03-01"

result
result |> format_table