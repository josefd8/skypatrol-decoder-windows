Imports System.Net
Imports System.Net.Sockets

''' <summary>
''' Clase para el manejo e interpretacion de mensajes de equipos Skypatrol
''' </summary>
''' <remarks></remarks>
Public Class SkyPatrol

    Private Registro As libreria.BaseDatos.Registro
    Private Paquete_b() As Byte
    Private fun As New libreria.ClassBit

    Private Enum T_Mensaje
        Posicion
        Panico
        Respuesta_Poll
        ShortMessage
        Actualiza_Cola
    End Enum

    Public ReadOnly Property Registro_Vehiculo() As libreria.BaseDatos.Registro
        Get
            Return Registro
        End Get
    End Property

    ''' <summary>
    ''' Crea un nuevo objeto SkyPatrol para la interpretacion de mensajes
    ''' </summary>
    ''' <param name="Datos_In">Mensaje transmitido por el equipo Skypatrol como una cadena de bytes</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal Datos_In() As Byte)
        Paquete_b = Datos_In
    End Sub

    ''' <summary>
    ''' Decodifica el paquete pasado en el constructor de la clase
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub Decodificar()
        Interpretar_SkyPatrol(Paquete_b)
    End Sub

    ''' <summary>
    ''' Decodifica el mensaje del equipo Skypatrol
    ''' </summary>
    ''' <param name="Datos"></param>
    ''' <remarks></remarks>
    Private Sub Interpretar_SkyPatrol(ByVal Datos() As Byte)
        Try
            Dim TempStr As String

            Select Case Datos.Length
                Case 37
                    Registro.Id_Vehiculo = CalcularId(Datos, 15, UBound(Datos) - 15)
                    Registro.Tipo_Mensaje = 99
                    Registro.Erroneo = False
                    Exit Sub
                Case 4
                    Registro.Tipo_Mensaje = 99
                    Registro.Erroneo = False
                    Exit Sub
            End Select

            Registro.Id_Vehiculo = CalcularId(Datos, 8, 22)

            Registro.In_Puertos = fun.Decimal_a_Binario(CDbl(Calcular(Datos, 31, 1)), 8)

            'GPIO direction
            TempStr = fun.Decimal_a_Binario(CDbl(Calcular(Datos, 30, 1)), 8)

            'ADC 1
            Registro.AD1 = Calcular(Datos, 32, 2) / 1000

            'ADC 2
            Registro.AD2 = Calcular(Datos, 34, 2) / 1000

            'Registro.In_Puertos.Last 
            If Registro.In_Puertos.Substring(7, 1) = "1" Then
                Registro.Encendido = "True"
            Else
                Registro.Encendido = "False"
            End If

            CalcularFecha(Datos, 37, 3, Registro.Fecha_date)
            CalcularHora(Datos, 52, 3, Registro.Fecha_date)

            'Latitud
            Latitud(Datos, 41, 3)

            'Longitud
            Longitud(Datos, 44, 4)

            'Velocidad
            Registro.Velocidad = Calcular(Datos, 48, 2) / 10
            Registro.Velocidad = Registro.Velocidad * 1.852

            'Direccion
            Registro.Direccion = Calcular(Datos, 50, 2) / 10

            'Altitud
            Registro.Altitud = Calcular(Datos, 55, 3)

            'Satelites
            Registro.Nro_Satelites = Calcular(Datos, 58, 1)

            'Estatus del GPS
            Registro.GPS_Valido = Calcular(Datos, 40, 1)

            Select Case UBound(Datos)
                Case 62
                    Registro.Odometro = Math.Abs(Val(Calcular(Datos, 59, 4)))
                    Registro.RTC = ""
                Case 68, 69
                    Registro.Odometro = Math.Abs(Val(Calcular(Datos, 59, 4)))
                    Registro.RTC = Calcular(Datos, 63, 6)
                    Registro.RTC = Val((2000 + Datos(63))) & "-" & Datos(64) & "-" & Datos(65) & " " & Datos(66) & ":" & Datos(67) & ":" & Datos(68)
                Case Else
                    Registro.Odometro = "0"
                    Registro.RTC = "0"
            End Select

            Registro.Tipo_Mensaje = Calcular(Datos, 4, 4)

            Registro.Erroneo = False

        Catch ex As Exception
            Registro.Erroneo = True
            Registro.Descripcion_Evento = Err.Description
        End Try
    End Sub

    ''' <summary>
    ''' Calcula el ID que el equipo transmite en su trama
    ''' </summary>
    ''' <param name="Datos">Paquete transmitido por el equipo</param>
    ''' <param name="inicio">Punto en la cadena donde empieza a leerse el ID</param>
    ''' <param name="Largo">Cantidad de bytes que se tomaran para leer el ID</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function CalcularId(ByRef Datos() As Byte, ByVal inicio As Integer, ByVal Largo As Integer) As String
        For j As Integer = inicio To Largo + inicio - 1
            CalcularId &= Chr(Val(Datos(j)))
        Next j
        Return CalcularId.Trim
    End Function

    ''' <summary>
    ''' Dado un conjunto de bytes, obtiene su valor en decimal
    ''' </summary>
    ''' <param name="Datos">Conjunto de bytes</param>
    ''' <param name="inicio">punto en donde se inicia la lectura de los bytes</param>
    ''' <param name="Largo">Cantidad de bytes a leer</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Calcular(ByRef Datos() As Byte, ByVal inicio As Integer, ByVal Largo As Integer) As String
        For j As Integer = inicio To Largo + inicio - 1
            Calcular &= fun.Rellena("0", 2, Hex(Datos(j)).ToString.Trim, "A")
        Next j
        Return CDbl("&H" & Trim(Calcular))
    End Function

    ''' <summary>
    ''' A partir de un arreglo de bytes, obtiene la fecha transmitida por el equipo
    ''' </summary>
    ''' <param name="Datos">conjunto de bytes de donde se obtendra la fecha</param>
    ''' <param name="inicio">Punto en el arreglo donde se iniciara la lectura</param>
    ''' <param name="Largo">Cantidad de bytes a leer</param>
    ''' <param name="fecha">Fecha inicial a la cual se añadiran los dias, meses y años</param>
    ''' <remarks></remarks>
    Private Sub CalcularFecha(ByRef Datos() As Byte, ByVal inicio As Integer, ByVal Largo As Integer, ByRef fecha As Date)
        Dim temp As String = ""
        For j As Integer = inicio To Largo + inicio - 1
            temp &= fun.Rellena("0", 2, Trim(CStr(Hex(Datos(j)))), "A")
        Next j
        temp = fun.Rellena("0", 6, CDbl("&H" & temp.Trim), "A")
        fecha = fecha.AddYears(CInt("20" & Mid(temp, 5, 2)) - 1)
        fecha = fecha.AddMonths(CInt(Mid(temp, 3, 2)) - 1)
        fecha = fecha.AddDays(CInt(Mid(temp, 1, 2)) - 1)
    End Sub

    ''' <summary>
    ''' A partir de un arreglo de bytes, obtiene la hora transmitida por el equipo
    ''' </summary>
    ''' <param name="Datos">conjunto de bytes de donde se obtendra la hora</param>
    ''' <param name="inicio">Punto en el arreglo donde se iniciara la lectura</param>
    ''' <param name="Largo">Cantidad de bytes a leer</param>
    ''' <param name="fecha">Fecha inicial a la cual se añadiran las horas, minutos y segundos</param>
    ''' <remarks></remarks>
    Private Sub CalcularHora(ByRef Datos() As Byte, ByVal inicio As Integer, ByVal Largo As Integer, ByRef fecha As Date)
        Dim temp As String = ""
        For j As Integer = inicio To Largo + inicio - 1
            temp &= fun.Rellena("0", 2, Trim(CStr(Hex(Datos(j)))), "A")
        Next j
        temp = fun.Rellena("0", 6, CDbl("&H" & temp.Trim), "A")
        fecha = fecha.AddHours(CInt(Mid(temp, 1, 2)))
        fecha = fecha.AddMinutes(CInt(Mid(temp, 3, 2)))
        fecha = fecha.AddSeconds(CInt(Mid(temp, 5, 2)))
    End Sub

    ''' <summary>
    ''' A partir de un arreglo de bytes, obtiene la latitud transmitida por el equipo
    ''' </summary>
    ''' <param name="Datos">Conjunto de bytes de donde se obtendra la latitud</param>
    ''' <param name="inicio">Punto en el arreglo donde se iniciara la lectura</param>
    ''' <param name="Largo">Cantidad de bytes a leer</param>
    ''' <remarks></remarks>
    Private Sub Latitud(ByRef Datos() As Byte, ByVal inicio As Integer, ByVal Largo As Integer)
        Dim resto As Double
        Dim Parte_Entera As Integer
        Dim Latitud As Double
        Dim Valor As Long

        Valor = Calcular(Datos, 41, 3)
        If Valor > 8388607 Then
            Valor = Valor - 16777215
        End If

        Latitud = Valor / 100000

        Parte_Entera = Fix(Latitud)
        resto = Latitud - Parte_Entera
        resto = resto / 60 * 100
        Registro.Latitud.Coordenada = Parte_Entera + resto

        If Registro.Latitud.Coordenada >= 0 Then
            Registro.Latitud.Cardinalidad = "N"
        Else
            Registro.Latitud.Cardinalidad = "S"
        End If
    End Sub

    ''' <summary>
    ''' A partir de un arreglo de bytes, obtiene la Longitud transmitida por el equipo
    ''' </summary>
    ''' <param name="Datos">Conjunto de bytes de donde se obtendra la Longitud</param>
    ''' <param name="inicio">Punto en el arreglo donde se iniciara la lectura</param>
    ''' <param name="Largo">Cantidad de bytes a leer</param>
    ''' <remarks></remarks>
    Private Sub Longitud(ByRef Datos() As Byte, ByVal inicio As Integer, ByVal Largo As Integer)
        Dim resto As Double
        Dim Parte_Entera As Integer
        Dim Longitud As Double
        Dim Valor As Long

        Valor = Calcular(Datos, 44, 4)
        If Valor > 2147483647 Then
            Valor = Valor - 4294967295.0#
        End If

        Longitud = Valor / 100000

        Parte_Entera = Fix(Longitud)
        resto = Longitud - Parte_Entera
        resto = resto / 60 * 100
        Registro.Longitud.Coordenada = Parte_Entera + resto

        If Registro.Longitud.Coordenada >= 0 Then
            Registro.Longitud.Cardinalidad = "E"
        Else
            Registro.Longitud.Cardinalidad = "W"
        End If
    End Sub

End Class
