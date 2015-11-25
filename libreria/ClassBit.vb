

''' <summary>
''' Clase para el manejo de datos binarios
''' </summary>
''' <remarks></remarks>
Public Class ClassBit

    ''' <summary>
    ''' Tranforma Un numero decimal en una cadena binaria
    ''' </summary>
    ''' <param name="Numero_Decimal">Número decimal</param>
    ''' <param name="Max_Caracteres">Largo Binario de retorno</param>
    ''' <returns>Representaicion en binario del numero decimal</returns>
    ''' <remarks></remarks>
    Public Function Decimal_a_Binario(ByVal Numero_Decimal As Double, ByVal Max_Caracteres As Integer) As String
        Dim Binario As String = ""
        Dim i As Int64 = Numero_Decimal
        Binario = Convert.ToString(i, 2)
        Binario = New String("0", Max_Caracteres - Binario.Length) & Binario
        Return Binario
    End Function

    ''' <summary>
    ''' Convierte un numero hexadecimal pasado como string en su representacion binaria
    ''' </summary>
    ''' <param name="cadena_hex">Cadena hex a convertir</param>
    ''' <returns>Representacion en binario</returns>
    ''' <remarks></remarks>
    Public Function Hex_a_Binario(ByVal cadena_hex As String) As String
        Dim hexToInt As Integer = Convert.ToInt32(cadena_hex, 16)
        Dim Binario As String = ""
        Binario = Convert.ToString(hexToInt, 2)
        Return Binario
    End Function

    ''' <summary>
    ''' Rellena por la izquierda con el caracter especificado
    ''' </summary>
    ''' <param name="Char_Relleno"></param>
    ''' <param name="Longitud_Necesaria"></param>
    ''' <param name="Valor"></param>
    ''' <param name="Lugar"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function Rellena(ByVal Char_Relleno As String, ByVal Longitud_Necesaria As Byte, ByVal Valor As String, ByVal Lugar As String) As String
        Rellena = ""
        If Lugar = "A" Then
            Rellena = Rellena.PadLeft(Longitud_Necesaria - Valor.Length, Char_Relleno) & Valor
        Else
            Rellena = Valor & Rellena.PadLeft(Longitud_Necesaria - Valor.Length, Char_Relleno)
        End If
    End Function


End Class
