﻿using PersonalFinances.Models;
namespace PersonalFinances.App;

public interface ITransactionsUserInteraction
{
    void ShowMessage(string message);
    void Exit();
    string GetInput();
    void OutputTransactions(List<Transaction> transactions);
    KeyValuePair<string, string>? PromptForVendorKVP(string transactionData);
}
