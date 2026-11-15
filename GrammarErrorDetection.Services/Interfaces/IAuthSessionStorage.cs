using GrammarErrorDetection.Models.Auth;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IAuthSessionStorage
{
    AuthSessionSnapshot? Read();
    void Write(AuthSessionSnapshot snapshot);
    void Remove();
}
