using System.Security.Cryptography;

namespace StreetwearApi.Services;

// Gera e confere hash de senha usando PBKDF2 (padrão do .NET, sem depender de pacotes externos)
public class SenhaService
{
    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;
    private const int Iteracoes = 100_000;

    public (string hash, string salt) GerarHash(string senha)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(senha, saltBytes, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public bool VerificarSenha(string senha, string hashArmazenado, string saltArmazenado)
    {
        var saltBytes = Convert.FromBase64String(saltArmazenado);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(senha, saltBytes, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
        var hashCalculado = Convert.ToBase64String(hashBytes);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(hashCalculado),
            Convert.FromBase64String(hashArmazenado));
    }
}
