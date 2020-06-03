﻿using APIClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainRemote
{
    [ServiceContract]
    public interface RemoteInterface
    {
        [OperationContract]
        List<Block> GetCurrentChain();

        [OperationContract]
        void AddBlock(Block block);

        [OperationContract]
        Block GetLatestBlock();

        [OperationContract]
        TransactionStruct ReceiveTransaction(TransactionStruct tr);

        [OperationContract]
        void SetChain(List<Block> chain);

        [OperationContract]
        float GetWalletBalance(uint userID);
    }
}
