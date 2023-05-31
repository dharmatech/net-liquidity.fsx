#r "nuget: CsvHelper, 30.0.1"

open System
open System.IO
open System.Net.Http
open System.Net.Http.Json
open System.Text.Json

open CsvHelper

let days = 365

// ----------------------------------------------------------------------
type TGARecordData = {
    record_date           : string
    open_today_bal        : int
}

type TGARecord = {
    data  : TGARecordData[]
}
// ----------------------------------------------------------------------

type RRPRecordRepoOperation = {
    operationId      : string
    operationDate    : string
    operationType    : string
    note             : string
    // totalAmtAccepted : int
    totalAmtAccepted : int64
}

type RRPRecordRepo = {
    operations : RRPRecordRepoOperation[]
}

type RRPRecord = {
    repo : RRPRecordRepo
}
// ----------------------------------------------------------------------
// ----------------------------------------------------------------------
let get_recent_tga (days : int) =

    let base_uri = "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/dts/dts_table_1?"
    let path = "tga.json"

    if File.Exists(path) then         
        let data = JsonSerializer.Deserialize<TGARecordData[]>(File.ReadAllText(path))        
        let last_date = data[data.Length-1].record_date
        printfn "Retrieving records since: %s" last_date
        let uri = sprintf "%sfilter=record_date:gt:%s,account_type:eq:Treasury General Account (TGA) Closing Balance&fields=record_date,open_today_bal&page[number]=1&page[size]=300" base_uri last_date
        let result_alt = (new HttpClient()).GetFromJsonAsync<TGARecord>(uri).Result

        if result_alt.data.Length > 0 then
            printfn "New records retrieved: %d" result_alt.data.Length
            let new_data = Array.append data result_alt.data
            File.WriteAllText(path, JsonSerializer.Serialize new_data)
            new_data
        else
            printfn "No new records retrieved"
            data        
    else
        printfn "Retrieving TGA data"
        let date = DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd")
        let uri = sprintf "%sfilter=record_date:gt:%s,account_type:eq:Treasury General Account (TGA) Closing Balance&fields=record_date,open_today_bal&page[number]=1&page[size]=300" base_uri date    
        let data = (new HttpClient()).GetFromJsonAsync<TGARecord>(uri).Result.data
        printfn "Retrieved %d records" data.Length
        File.WriteAllText(path, JsonSerializer.Serialize data)
        data
// ----------------------------------------------------------------------
let get_recent_rrp (days : int) =
    
    let path = "rrp.json"

    if File.Exists(path) then         
        let data = JsonSerializer.Deserialize<RRPRecordRepoOperation[]>(File.ReadAllText(path))
        let last_date = data[data.Length-1].operationDate
        let date = DateTime.Parse(last_date).AddDays(1).ToString("yyyy-MM-dd")
        printfn "Retrieving records since: %s" date
        // let uri = sprintf "%sfilter=record_date:gt:%s,account_type:eq:Treasury General Account (TGA) Closing Balance&fields=record_date,open_today_bal&page[number]=1&page[size]=300" base_uri last_date
        let uri = sprintf "https://markets.newyorkfed.org/api/rp/reverserepo/propositions/search.json?startDate=%s" date
        let result_alt = (new HttpClient()).GetFromJsonAsync<RRPRecord>(uri).Result.repo.operations

        if result_alt.Length > 0 then
            printfn "New records retrieved: %d" result_alt.Length
            let new_data = Array.append data result_alt
            File.WriteAllText(path, JsonSerializer.Serialize new_data)
            new_data
        else
            printfn "No new records retrieved"
            data        
    else
        printfn "Retrieving RRP data"
        let date = DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd")
        let uri = sprintf "https://markets.newyorkfed.org/api/rp/reverserepo/propositions/search.json?startDate=%s" date
        let data = (new HttpClient()).GetFromJsonAsync<RRPRecord>(uri).Result.repo.operations |> Array.sortBy (fun item -> item.operationDate)
        printfn "Retrieved %d records" data.Length       
        File.WriteAllText(path, JsonSerializer.Serialize data)
        data
// ----------------------------------------------------------------------





// ----------------------------------------------------------------------

let tga = get_recent_tga days
let rrp = get_recent_rrp days










// ----------------------------------------------------------------------
#r "nuget: Fli, 1.0.1"
open System.Text.Json
open Fli


let format_table seq =
    let json = JsonSerializer.Serialize seq
    System.IO.File.WriteAllText("c:/temp/out.json", json)
    let result_cli = (cli {
        Shell PS
        Command "Get-Content c:\\temp\\out.json | ConvertFrom-Json | Format-Table"
    } |> Command.execute)

    match result_cli.Text with
    | Some(txt) -> printfn "%s" txt
    | None -> printfn "issue"

// let seq = tga.data

// format_table tga.data

// ----------------------------------------------------------------------
open System.Reflection
open FSharp.Reflection

let printTable<'T> (items: seq<'T>) =
    let genArgs = items.GetType().GenericTypeArguments
    assert (genArgs.Length = 1)
    let itemType = genArgs[0]
    assert (FSharpType.IsRecord itemType)
    let fieldNames = FSharpType.GetRecordFields itemType |> Array.map (fun propInfo -> propInfo.Name)
    printfn $"""{fieldNames |> Array.iter (printf "%10s")}"""
    printfn $""" {Array.replicate fieldNames.Length "_________" |> String.concat " "}"""
    let printFields item =
        let fields =  FSharpValue.GetRecordFields item
        printfn $"""{fields |> Array.map string |> Array.iter (printf "%10s")}"""
    items |> Seq.iter printFields


// printTable tga.data
// ----------------------------------------------------------------------





        // let filters = [
        //     $"record_date:gt:%s{last_date}"
        //     "account_type:eq:Treasury General Account (TGA) Closing Balance" ] |> String.concat ","

        // let fields = [
        //     "record_date"
        //     "open_today_bal" ] |> String.concat ","

        // let uri = sprintf "%sfilter=%s&fields=%s&page[number]=1&page[size]=300" base_uri filters fields