using System.Collections.Generic;
using System.IO;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Textures;
using Dalamud.Utility;
using Lumina.Text.ReadOnly;
using OmenTools.Info.Dalamud;

namespace DailyRoutines.Common.Info;

public static class Assets
{
    public static ISharedImmediateTexture Icon { get; } =
        ITextureProvider.Instance().GetFromFile(Path.Join(IDalamudPluginInterface.Instance().AssemblyLocation.DirectoryName, "Assets", "icon.png"));

    public static ISharedImmediateTexture BackgroundTexture { get; } =
        ITextureProvider.Instance().GetFromFile(Path.Join(IDalamudPluginInterface.Instance().AssemblyLocation.DirectoryName, "Assets", "background.png"));

    public static List<StyleInfo> Styles { get; } =
    [
        new
        (
            "Tropical Sanctuary",
            "DS1H4sIAAAAAAAACpWYTXPiOBCG/8qWz6mUvizL3CbD7uQw2UpN2JrdvQmjgAdjM8aQj6n895GwWpaxIcoJI/qRul+12i1+RTKa4Gt0Fc2jya/oX/2FmG//6Qd0jd6uogyGFtEEmU9lDdF1fLTTn9rsUZtdRUswXlnjHAbk3I78gHWQXYcfl1nrkaMfhbXbWDvRMyvH6WrUeAvGENFx9Gc0IUe61iPcPOxgvgYe9vBwiCbUfD5BHM/WvZdRFV7dKPJGpbTDvOeezCwuFx1GUSoQTbHFEccpR4mgV9H/5jvGOCYp0z9/P/4sKE1TIcxs3b4gRmlCYnAMCZ6wGMMMTCQoFsLNgPX0jJoZHgfeIwshsDYJURXGbqaeG2efMguk4khgS7SKG9NpvpPzQnWRckskdolEeMT3vFxUTzfLzqGYCSqQSCyFCWKMcYD1N46R8cJN8XmVFwt/hiRNiCCwLOYMEw6ykoTzOEHMhsk0f19t99seDyQDCMxTo/9NVS9UHaRga/qwkjrGIOCvWm6U5wtGbVZAMMQylOCYC+7LYNHb6qBqT31MLMogGs4hmo76lDX5QQ2hBBZMARImz2d5U6jepoFkkHv+HlvrkzXOytyDPldFIbc7L6B3uDtV7m9k7TvnMgGCIZ79Q1brJeZ9grkD5c7SGPKllvOhZibzjtBZ5uwexXBE+Dk0dKPgaKhsfSfrtQNAvNRp0ROjyHW+joc1TIU07iE917BL84SNaXizb5qqHBhzWATqXkqccV+1jhFkLOla5oxPjPeWaYlbJXvnWrgCAlL5B6e1Dt1JPRA7KPywcSOw2spaNlXnGTsVSxBrj337s74N1yE+F7iPRg+NfVO7/FV9qfNtgAiE9IgPeMj65AckNODMS+jL5WAWfjrtns4Gh7LLHCgftL/CP+Vjle17Re1yAfWQj1XRaZWt83J5X6tDrp5C9KId9edm27wEV/n7omq+5qXahRYnB3zsYBvsNt811VK/ugIKSHzKnFtutCjMTC/THtuQFsGD7Lu/qatyGXJ0eR/8mi9XTQCHBHDf+n3UhS6jM/9UNCF9DxG2uXtQhcoa5bdbl8qKaa+mtVxO62o7k/VSNe9ucltd/5aHWy1A4YlwCYlbpG0mdeaespe9FCf0NN8ECElNYbmrFrJowTAqJm/mcqObrmgSzbQseSaLPx5kmTV7Wb9E+vbVdt1yEDZxPbBfTrrCxsmlRjvrcsndY1xH6FfArigRqGPUdTSxZ6gCLwXdZcO1seloQVi+d5pbs1VogckDu54fgVKvh5nIR14hRbcj9LS0pL6Am8CtKwPjqEKF2YZ6+DNsR+qhgPCujP3pdkMBIQWpb9eEBrLvcvU0pxMUe3e8Q6CE3SuSpRCxc5H6Mz6HSdP9e8DhkJiZ7V3Un/A1MAvdfwzelBSD3tS/2bqu5x0nZRbopVHStMjo7TcBWyY7TxIAAA=="
        ),
        new
        (
            "Code Cobalt",
            "DS1H4sIAAAAAAAACqWYS3PiOBCA/0rKZ3ZKlmRL5jZJdieHyVYqydZs5iZAAQ8OZowhj6n899HbkgW74OQQCtH9uR/qVsu/EpaM009glEyS8a/k32RM5ZcH9fk+SqbJOJcLs2QM5Cc3UsBIgU+ZkHoUjFEyN7ILQyyTMZafbGLkfxjl3Chj9YilEauM1JORwkYqU1Kr3qrWrXurUK2u98r+FD8ruxphnzJhY7xtxYJ69Nbo7BRplDyb7y/GtFfnfeZ5/xbFJJXLjJl1ZNaRMoNNjT6bdXoE0iID2KgDSjDBIMtHyXeFgykuUoJGyTf9M0JFQamEdflIAQQZArk1weloBAGAUowdIgWEYiQRj5H5wCgBKy03Ql1JuXv+0jr5AqcpyW0oiiynOSSZUS7UN5wpRqoYUvmy3LBJxTvfcVrkwhZLwdKsFBFDyXABhADyKN/K1ax+Pp97nmuEc10YhQlw0dOG5B7iYlFWM58AU2k8sgCcgYIC6wn0NG/q9XbtazrRh/BZWlMGQPzuAc7rZsYbp99JKH2Yqz9q9JGj9/TvFkzEoEucEkOFdQCoyKc2jS5PjvJXw56474aNmLZCoEREbA4gARmlcI/+Vb3jjZdLCFEGxdMsR2fPeaOjjGPO52lb7rytrNKJrTcdVWNUuv2Y3pdtFXgTupMq97E1o6P3AX0zJCSjhcU4PRMVlRwYYy7qqmLrjReXk0nXfLU9Z43vEyIynKnbaIoALUJFFvpBuZs2wo5JCNHbBLoeIRMEXYswQYsRXxo2Gb5lA0y0ZVR9W7eQ/sMGhpWTKT4E6yVMbxPrnYPo7YuD+ufT5TVrlgfMyFS+CgOgUSO7q0pRhGFUBgL+04WgjOMqPt+2bb06lBisy8amN5dBQFmsH2VEC2LXkG34FIbERawx/eJRGy21NYigMt+1dZ1nf7NdceY3xpNLRut/uCNpzAcb0h1fs4a19aA+30XHJ324bixoaM3c8k35xr805frkPS94AaDvy0mbvsP0POmFOLNZ0ebolPnN2ive0zv9nkZ2atncR8UPKQIFIQ4BMUHEDVSivdM8K/IQ8c/qsZ5ug8Pm5NPPo/QtcufKQ5Dw72GDcajLerosV/Obhu9K/jywORrIn0/r9vVDY95NVbdfyxXfDNtpTr2fahxGJfeikpqhuSNclZu2nos5p1MPraDaEdsZ5eSVF4cgkSn6QHcsWwKm5evj3c90xXWH+8hooTBmEG2bejUfPht4qK/lfNEOnkgV6HbwvQD0IJ+rdkh8dKeT95w7XvFpy/1bxvHtEsoqaNj8sqnX96yZc2tM6u5o+pYE8+CqpkPxN9tdiVhWQTxP2vmCoG9Zogz7qGNNcIDL8mlwUuQd97qesUrTQtQJtx7h8bt8iSBuG8k4uWRl9Xp2W29bWd1nf5xd1DMu/k2YyPoomemrLoscBns87Q4RYqTsJiWe1PQoqVn8esF2BU+K9+xKjV1h6+lu9ZRQ1x3ccUj8ep5HnhJQ7PF1Eb0pyPc8uYyk3NmjpxRP9kcXF3tcHoz08oDfampxUlVH7Ie6CPa414/7w48ntfKP1d5jfePqyGmMij3B6YanHBWuuTkLg+D8jNIiZpo9z+6mTOrKCjummPs92e4opK5L2/QoupNsj0r21pt/rJF20xJ/+tudsCueY2huoSjVZ4qHfjly+3Zv74jDAuze8vjWvh1V++4Nn0eEe3lu1vw/G9n0WCNlTOV9Cbz/Bu3VCfDEFQAA"
        ),
        new
        (
            "Nebula Serene",
            "DS1H4sIAAAAAAAACqWYW3ObOhCA/0qGZ5+OJIQAvzV12zw0nU6SMz3tm4wVm5qAi7Fz6eS/V3eJS+bY4BdA3v20u1qtLn8CGszhOzALlsH8T/BfME/Exw/5fJ0FWTAnomEVzIF4Mi0FtBR4F3Gpe86YBetgjkXzRv+d62+61A2/tDLRylh2sdVihZZ60FK4JVUOtladViRbd7oVtVp/8xfpac2tlSbstWLDG2TXB91w1M9H/XzSpj1b7yPP+5deTER3lA5aTDOtTldWDQIEohAQrQ0BxglKZ8FP+RVCBCGIZ8F3iU7CME2TRKDcYEAM0hBgaAg4DAlA2CBiAJIEY4vguASHAnHft10rASMtsqAqhNwde2qsPCEoxpHpkaQYwpjEWjkJUwQjZTOUCKG7yPd0WTDnOEZhhFKMNAQbLcmIUIRTgDzG97xcVY+Xay9wMCXcFxe5KEk40zrOAVwCeowPm7xYeQiQtPxwnwrAfcKxGBkL+FbtDjvfBivyo/2pAAkQTvmAy6pesdrqoxCGMYpMQrlPqR9KD5Oe+u2G8lA4LyKQip5C44b7VoNJ5M8fkE81fWC+H0QmhRkLZ7gEIBvYLuCqOrLaG1Mk4sd7H/ZH/4n6nPdZkx9dRiMzcoqi0stYEwokTv30ypui7Q0fhChJjTdEjqv1Ro0S7AO6VpzrjMZ8qIqC7vZTwnLNysMlraf4dJvV3I5lC3J2vlvI55ouR2dti9JLGZmryCSv1VYs6+YwrDNioQySiVHYjhGW5QaiVj1g2faa1ltXlMQ04O6YyqZ0TElJFTHyzSlyPiNb4ZnA6PhDYMSlTWhUmgAVZ6hrtK4Lh6apyvHTWelPns0KM20yXzHqV8iz817pT3ZFYaa5cst2tKZNNb7eW8LkWWNAE2fMDdvnL+xzne/Gp7tjdL06Od8douNPLDcB2BBU4scDhDtvwp6/XgyUsbMXi95sP3fVo8t/y/sqO/grzYhNkc/pWHR2YBZVts3L9beaHXP2OD5FNOfjw655nrRfK6rmS16y/XhTLGJ0sgrCVb5vqjXf7EwzxGJGG3MnNuGquE3aMQmM3o02dVWuR5c4j/QlX2+aiTbdTD0jOMr7ohmfeeLIc8sKljXMP3EgecohtuCq/pHmEBksH7Oo6XpRV7s7Wq9ZMz55vtLjFY9u0YrwmQWHM9QhjM/MPmyMSRa3yB+mjdp1taKFwrVZZx+OxHUDP5ME82BB8+L54qY6NGL6X/xz8ZUtDwW9uOUzr2TBLFipk7E568M3jtHKQrfcmBCZs65/MMtOklr1ryLMudmTYh27oLarXRDcJUASm9VLvv20b1Z23fM0BumAr5vexQIZ6DnvSSX22kP+PNlfb6zwQ5HevuE3z1FPqnDEbqhT4ofRK9nmqmIAV/p3Ap1ufeOqntM4TAeC4/ZXJDTVAgFrYSs4v3vDEgE40LfbhyZ2bmHLJJE/1G7FTOymxgyPpFvJ5qTBPriqY3PWJG0c6esm/Cqv307Oisc+lNiVBqqZ7aGfTkxfd9MXWyzAdi/nW/ty0ty314EeEQ3y7K70/2yk2alGipiKAxV4/Qs/CZTu8BUAAA=="
        ),
        new
        (
            "Cosmo Sharp",
            "DS1H4sIAAAAAAAACqVYSXPaShD+Ky6deSlpNCNpuMXmJT4kKZftV3nkNggZFAQiQuAllf+e2ReNwCxcoEX3N71Pt34HJBhGH8JBMAmGv4P/g2HGiDH//jMIcvlgGgxD9l1IrlByhR8Q5XqiGINgFgwT9nguEUspSyaS/2fnCMiPWEixSnItJRd0uFbyaeI8rXufrnsRfgVDANiThurHf2ykYEsf8KO3UredlH2WDC9StVdtPbKsf+v1CSHysThrLH6wP3IJQKZGMImjFKVSXFM/GBWBLM0iAAfBd/5nFscYZxlDMtGIQhCiOEwkgiE5BAgTFCOMNEQUphmMGcSTp3wohULFzdKgrhjfY/HSan4c4iyMsZTCEDEaSmGc8BM4RMQhmOyo3JBJVRi74zSBqdZaUxwChRFFySyI7+VqWj9fz4zKMQYRSrJMqW5o4ToOkWaJBXIzL6upjcH0jOJURdSiBQbTCWXYgrir19u1BRFFjhmGFM4HKfeNBXBdN9Oi0fKGY+ySXD5GAMMIeOIPc0K98W74pBup2KeGLAtbbcg1VccaUqjNzc7scyXAbb0rGiuIQLhdZa8hhfopQBCHsY/zMW/LnUlhkPCP9oImOQzkyQZsdR7LtnLsMdEed2hZBTy00IfoaGJCPnZJYZD49MDc1FVF1hvLNScjfS1W22vSXJJcD3lD9Zg4IGHC+CLdXyQl+wuLD+hD+NyQialVWlhUUNVJJrypykSEJ0pk1mUJwimGXbBu6sAM8PRWmCpAP+QVcgirEzTRcKJE9yPmKbQX6mZe5IuvpFmcY590U1XSMnR8hNSpHALKVqIammiRaS9E1xpWejFSSMY4jpRgltqR3RK2bVuvjC1CAKhaEn0R61ryWquQ98LjNqaYN4RYayFiF3swHVsgdrxiGgJHSb32dlsQuzueXENC3utSB5ssFP6OPZhuk1KeHzuUiK++OEx8izVpSFtb1rjV7BRz7IVFy3vW6Ij2BrinKyikjkH6zLFDCYOs0NCuIV1Ewe6LTflWfG7K9fnVYzDOaApdiNN7gWzcVu0ebrSasmW7mh8KLhDXKXIRuul1OEnFfdaB+G/1VOdb+9YJXQcack+/t0G6V6HoWUjPCIoUNrF8B8hWaFTni3I1u2uKXVk8n3d5mJFFov27XLevF859d1XdfilXxcYMsLyT4P5aVinjy7tBj/TgZaIju6O4Yx2A23LT1jM6/phUZXonscIQkQIKQyThPpBu+qXcCYmKOxJ9XjdaL1aPbBgXbe7CMYoByam0berV7OzR1kL6Us7m7Zl9k8PcX7wrGJiPVXvJxsCWn4eiKvK2sHePk6oCiAWLVUVDZqOmXj+SZla0Z+bzN7K7pR6uHC+fmI0UQ+xktEa7YKowIrmRmCFMVEZHfFQu7WDtWWbkhsgG5XpKKiF9mih7l0DXj2AYjEhZvV7d19uWlfXVP1c39WZZX9G9qlkHg2Aqdl7iFbqLLDxhrhG1Bqmpz/ZXfhTX1H/LoKJocRXv+FlwmfWeXqPmQpV4/JfmnXmWpiHusXXu+TrpObn0uHSliZHN4v1p/MIvSXjA04s9dtN0tbgqg9h1NXbKwHRidSjsgVvZU0LnWFu52jMaxrjHOWZuSvQbFBBqDR3n/PLCQu/HnrPNjJnp3g01Jl0oLF5zCWa6N2fmvYvF2R4V7K1p+DpnkR7wZQWy1rU7ISuefdBEgdLhjn8s6Jcj09e8xEvNegX1SGNr+3ZU7es3fRYi6MXT0+Z7OpL8WCWZT9myFP75C47813TLFQAA"
        )
    ];

    public static ReadOnlySeString BoxedLetterR
    {
        get
        {
            if (!field.IsEmpty)
                return field;

            using var rented = new RentedSeStringBuilder();
            rented.Builder
                  .PushColorType(34)
                  .Append(SeIconChar.BoxedLetterR.ToIconString())
                  .PopColorType();

            return field = rented.Builder.ToReadOnlySeString();
        }
    }

    public static ReadOnlySeString BoxedLetterD
    {
        get
        {
            if (!field.IsEmpty)
                return field;

            using var rented = new RentedSeStringBuilder();
            rented.Builder
                  .PushColorType(34)
                  .Append(SeIconChar.BoxedLetterD.ToIconString())
                  .PopColorType();

            return field = rented.Builder.ToReadOnlySeString();
        }
    }

    public static ReadOnlySeString BoxedLettersDR
    {
        get
        {
            if (!field.IsEmpty)
                return field;

            using var rented = new RentedSeStringBuilder();
            rented.Builder
                  .PushColorType(34)
                  .Append(SeIconChar.BoxedLetterD.ToIconString())
                  .Append(SeIconChar.BoxedLetterR.ToIconString())
                  .PopColorType();

            return field = rented.Builder.ToReadOnlySeString();
        }
    }

    public static ReadOnlySeString BracketDailyRoutines
    {
        get
        {
            if (!field.IsEmpty)
                return field;

            using var rented = new RentedSeStringBuilder();
            rented.Builder
                  .PushColorType(34)
                  .Append("[Daily Routines]")
                  .PopColorType();

            return field = rented.Builder.ToReadOnlySeString();
        }
    }

    public static ReadOnlySeString BracketDR
    {
        get
        {
            if (!field.IsEmpty)
                return field;

            using var rented = new RentedSeStringBuilder();
            rented.Builder
                  .PushColorType(34)
                  .Append("[DR]")
                  .PopColorType();

            return field = rented.Builder.ToReadOnlySeString();
        }
    }
}
