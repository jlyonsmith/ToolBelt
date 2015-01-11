using System;

namespace ServiceBelt
{
    public interface IHiddenDataManager
    {
        string Hide(string data);
        string Reveal(string hiddenData);
    }
}

