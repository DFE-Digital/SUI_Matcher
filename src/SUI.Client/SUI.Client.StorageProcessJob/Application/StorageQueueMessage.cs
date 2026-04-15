namespace SUI.Client.StorageProcessJob.Application;

public sealed record StorageQueueMessage(string MessageText, string MessageId, string PopReceipt);
