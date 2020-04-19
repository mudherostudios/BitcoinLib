using System;
using System.Collections;
using System.Collections.Generic;
using BitcoinLib.Services.Coins.XAYA;
using BitcoinLib.Requests;
using BitcoinLib.Responses;
using BitcoinLib.Requests.CreateRawTransaction;
using BitcoinLib.Responses.SharedComponents;

using Newtonsoft.Json;

namespace BitcoinLibDebugger
{
    class Program
    {
        static XAYAService service;
        static string rpcUsername = "xaya";
        static string rpcPassword = "bm4rxrmr8b";
        static string walletPassword = "X24a1y25a1h7a1x24!";
        static string port = "8396";
        static string daemonUrl;

        static string buyer = "p/Cairo Denji";
        static string seller = "g/iow";

        static void Main(string[] args)
        {
            daemonUrl = $"http://{rpcUsername}:{rpcPassword}@127.0.0.1:{port}/wallet/game.dat";
            service = new XAYAService(daemonUrl, rpcUsername, rpcPassword, null);

            //Construct Outputs and First RawTransaction Part
            GetShowNameResponse sellerInfo = service.ShowName(seller);
            var rawRequestA = new CreateRawTransactionRequest();
            rawRequestA.AddOutput(service.ShowName(buyer).address, 0.01m);
            rawRequestA.AddOutput(sellerInfo.address, 1.0m);
            string rawTransactionA = service.CreateRawTransaction(rawRequestA);

            //Build the Funded RawTransaction Automatically.
            string options = @"{ 'feeRate':0.001, 'changePosition': 2}";
            rawTransactionA = service.GetFundRawTransaction(rawTransactionA, options).Hex;

            //Build the Second RawTransaction Part with Inputs
            var rawRequestB = new CreateRawTransactionRequest();
            rawRequestB.AddInput(sellerInfo.txid, sellerInfo.vout);
            string rawTransactionB = service.CreateRawTransaction(rawRequestB);

            //Decode the Psbt Parts and build the new Ins and Outs
            var rawDecodedTransactionA = service.DecodeRawTransaction(rawTransactionA);
            var rawDecodedTransactionB = service.DecodeRawTransaction(rawTransactionB);
            List<CreateRawTransactionInput> fullIns = new List<CreateRawTransactionInput>();
            Dictionary<string, decimal> fullOuts = new Dictionary<string, decimal>();
            foreach (Vin vin in rawDecodedTransactionA.Vin)
                fullIns.Add(new CreateRawTransactionInput() { TxId = vin.TxId, Vout = int.Parse(vin.Vout) });
            foreach (Vin vin in rawDecodedTransactionB.Vin)
                fullIns.Add(new CreateRawTransactionInput() { TxId = vin.TxId, Vout = int.Parse(vin.Vout) });
            foreach (Vout vout in rawDecodedTransactionA.Vout)
                fullOuts.Add(vout.ScriptPubKey.Addresses[0], vout.Value);
            foreach (Vout vout in rawDecodedTransactionB.Vout)
                fullOuts.Add(vout.ScriptPubKey.Addresses[0], vout.Value);

            //Combine the Ins and Outs into a new RawTransaction
            var combinedTransactionRequest = new CreateRawTransactionRequest(fullIns, fullOuts);
            string combinedTransaction = service.CreateRawTransaction(combinedTransactionRequest);

            //Update the RawTransaction with a Required Name Operation from the Seller
            string transactionPsbt = "PSBT Not Set";
            NameOperation nameOperation = new NameOperation("name_update", seller, "Atomic Transfer Successful!");
            NameRawTransactionResponse namedResponse = service.NameRawTransaction(combinedTransaction, nameOperation);
            if (namedResponse == null)
            {
                Print("Name Operation Failed");
                return;
            }
            else transactionPsbt = service.ConvertToPsbt(namedResponse.Hex);
            string buyerSigned = service.WalletProcessPsbt(transactionPsbt).Psbt;

            //Connect to a different wallet to act as the seller.
            daemonUrl = $"http://{rpcUsername}:{rpcPassword}@127.0.0.1:{port}/wallet/vault.dat";
            service = new XAYAService(daemonUrl, rpcUsername, rpcPassword, walletPassword);

            string sellerSigned = service.WalletProcessPsbt(transactionPsbt).Psbt;
            string cosigned = service.CombinePsbt(new List<string>() { buyerSigned, sellerSigned });
            FinalizePsbtResponse finalized = service.FinalizePsbt(cosigned);

            daemonUrl = $"http://{rpcUsername}:{rpcPassword}@127.0.0.1:{port}/wallet/game.dat";
            service = new XAYAService(daemonUrl, rpcUsername, rpcPassword, null);

            if (finalized.Complete)
            {
                string txid = service.SendRawTransaction(finalized.Hex, false);
                Print(txid);
            }
            else
            {
                Print("Finalization Not Compelte!");
            }
        }

        static Dictionary<string, decimal> BuildOutputs(decimal price, out GetShowNameResponse sellerInfo)
        {
            Dictionary<string, decimal> outputs = new Dictionary<string, decimal>();
            sellerInfo = service.ShowName(seller);
            string buyerAddress = service.ShowName(buyer).address;
            outputs.Add(buyerAddress, 0.01m);
            outputs.Add(sellerInfo.address, price);
            return outputs;
        }

        static void AddOutputs(Dictionary<string, decimal> outputs, CreateRawTransactionRequest request)
        {
            foreach (KeyValuePair<string, decimal> pair in outputs)
                request.AddOutput(pair.Key, pair.Value);
        }

        static void Print(string printable)
        {
            Console.WriteLine(printable);
            Console.ReadLine();
        }
    }

    public class NameOperation
    {
        //{"op":"name_update","name":"{sellerInfo.name}","value":"Atomic Transfer Successful!"
        public string op;
        public string name;
        public string value;

        public NameOperation() { }
        public NameOperation(string _operation, string _name, string _value)
        {
            op = _operation;
            name = _name;
            value = _value;
        }
    }
}
