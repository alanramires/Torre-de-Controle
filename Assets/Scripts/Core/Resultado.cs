namespace TorreDeControle.Core
{
    /// <summary>
    /// Resposta de um comando: deu certo, ou o motivo de não ter dado. A tela mostra o motivo;
    /// nada de exceção para erro de uso (nome vazio, repetido...).
    /// </summary>
    public readonly struct Resultado
    {
        public bool Ok { get; }
        public string Erro { get; }

        Resultado(bool ok, string erro)
        {
            Ok = ok;
            Erro = erro;
        }

        public static Resultado Sucesso() => new Resultado(true, null);
        public static Resultado Falha(string erro) => new Resultado(false, erro);
    }
}
