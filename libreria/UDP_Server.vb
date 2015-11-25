Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports Libreria.ClassGeneral

''' <summary>
''' Clase para el manejo de conexiones UDP en modo servidor
''' </summary>
''' <remarks></remarks>
Public Class UDP_Server

#Region "Variables"
    ''' <summary>
    ''' Variable de objeto que contiene el socket
    ''' </summary>
    ''' <remarks></remarks>
    Private ElSocket As New Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
    ''' <summary>
    ''' Variable que contiene al hilo encargado de recibir los datos
    ''' </summary>
    ''' <remarks></remarks>
    Private HiloRecibir As Thread
    ''' <summary>
    ''' Variable que indica si el programa se está cerrando
    ''' </summary>
    ''' <remarks></remarks>
    Public Saliendo As Boolean = False
    ''' <summary>
    ''' Variable temporales para almacenar los datos recibidos
    ''' </summary>
    ''' <remarks></remarks>
    Public Ip_Remoto As String
    ''' <summary>
    ''' Variable temporales para almacenar los datos recibidos
    ''' </summary>
    ''' <remarks></remarks>
    Public Puerto_Remoto As Integer
    ''' <summary>
    ''' Variable temporales para almacenar los datos recibidos
    ''' </summary>
    ''' <remarks></remarks>
    Public Estado As Boolean = False
    ''' <summary>
    ''' Variable temporales para almacenar los datos recibidos
    ''' </summary>
    ''' <remarks></remarks>
    Public Id_Unico As Integer
#End Region

#Region "Eventos"
    ''' <summary>
    ''' Este evento se dispara de forma asincrona cuando llega un mensaje de un cliente remoto
    ''' </summary>
    ''' <param name="Datos">Detalles del cliente y del mensaje transmitido como un objeto Paquete_Entrante</param>
    ''' <remarks></remarks>
    Public Event Datos_Recibidos(ByVal Datos As Paquete_Entrante)
    ''' <summary>
    ''' Este evento se dispara de forma asinrona cuando se presenta un error en el socket
    ''' </summary>
    ''' <param name="Mensaje">Mensaje de error</param>
    ''' <remarks></remarks>
    Public Event Error_Socket(ByVal Mensaje As String)
#End Region

    ''' <summary>
    ''' Envia un mensaje constituido por un arreglo de bytes a un equipo remoto
    ''' </summary>
    ''' <param name="DatosBytes">Arreglo de bytes a enviar</param>
    ''' <param name="Ip">IP del cliente remoto como un string</param>
    ''' <param name="Puerto">Puerto del cliente remoto como un String</param>
    ''' <remarks></remarks>
    Public Sub Enviar(ByVal DatosBytes() As Byte, ByVal Ip As String, ByVal Puerto As Integer)
        Try
            Dim Destino As New IPEndPoint(IPAddress.Parse(Ip), Puerto)
            ElSocket.SendTo(DatosBytes, DatosBytes.Length, SocketFlags.None, Destino)
        Catch ex As Exception
            RaiseEvent Error_Socket("Enviar " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Envia un mensaje constituido por un string a un equipo remoto
    ''' </summary>
    ''' <param name="DatosStr">Mensaje a enviar como un String</param>
    ''' <param name="Ip">IP del cliente remoto como un string</param>
    ''' <param name="Puerto">Puerto del cliente remoto como un String</param>
    ''' <remarks></remarks>
    Public Sub Enviar(ByVal DatosStr As String, ByVal Ip As String, ByVal Puerto As Integer)
        Try
            Dim Destino As New IPEndPoint(IPAddress.Parse(Ip), Puerto)
            Dim DatosBytes As Byte() = Encoding.Default.GetBytes(DatosStr)
            ElSocket.SendTo(DatosBytes, DatosBytes.Length, SocketFlags.None, Destino)
        Catch ex As Exception
            RaiseEvent Error_Socket("Enviar " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Envia un mensaje constituido por un string a un equipo remoto
    ''' </summary>
    ''' <param name="DatosStr">Mensaje a enviar</param>
    ''' <param name="destino">Identifiador del cliente remoto como un objeto IPEndPoint</param>
    ''' <remarks></remarks>
    Public Sub Enviar(ByVal DatosStr As String, ByVal destino As IPEndPoint)
        Try
            Dim DatosBytes As Byte() = Encoding.Default.GetBytes(DatosStr)
            ElSocket.SendTo(DatosBytes, DatosBytes.Length, SocketFlags.None, destino)
        Catch ex As Exception
            RaiseEvent Error_Socket("Enviar " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Envia un mensaje constituido por un arreglo de bytes a un equipo remoto
    ''' </summary>
    ''' <param name="DatosBytes">arreglo de bytes a enviar</param>
    ''' <param name="destino">Identifiador del cliente remoto como un objeto IPEndPoint</param>
    ''' <remarks></remarks>
    Public Sub Enviar(ByVal DatosBytes() As Byte, ByVal destino As IPEndPoint)
        Try
            ElSocket.SendTo(DatosBytes, DatosBytes.Length, SocketFlags.None, destino)
        Catch ex As Exception
            RaiseEvent Error_Socket("Enviar " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Cierra el soket UDP
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub Cerrar()
        Try
            Me.ElSocket.Close() 'Cierra el socket
            Me.Saliendo = True 'Indica que se está saliendo del programa
            If Not Me.HiloRecibir Is Nothing Then
                Me.HiloRecibir.Abort() 'Termina el proceso del hilo
            End If
            Estado = False
        Catch ex As Exception
            If Not Me.Saliendo Then
                RaiseEvent Error_Socket("Cerrar " & ex.Message)
            End If
        End Try
    End Sub

    Public Sub Iniciar(ByVal Ip As String, ByVal Puerto As Integer)
        Me.Iniciar(System.Net.IPAddress.Parse(Ip), Puerto)
    End Sub

    ''' <summary>
    ''' Inicia la escucha por el socket UDP
    ''' </summary>
    ''' <param name="Ip"></param>
    ''' <param name="Puerto"></param>
    ''' <remarks></remarks>
    Public Sub Iniciar(ByVal Ip As Net.IPAddress, ByVal Puerto As Integer)
        Try

            ElSocket.Bind(New IPEndPoint(Ip, Puerto))
            ElSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, True)

            HiloRecibir = New Thread(AddressOf Me.RecibirDatos) 'Crea el hilo
            HiloRecibir.Start() 'Inicia el hilo
            Estado = True
        Catch ex As Exception
            RaiseEvent Error_Socket("Iniciar: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Hilo que espera conexiones y recibe mensajes de los clientes
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub RecibirDatos()
        'Mientras el inidicador de salida no sea verdadero
        Do Until Saliendo

            'Variable para obtener la IP de la máquína remitente
            Dim LaIPRemota As New IPEndPoint(IPAddress.Any, 0)
            'Variable para almacenar la IP temporalmente
            Dim IPRecibida As EndPoint = CType(LaIPRemota, EndPoint)
            Dim RecibirBytes(50000) As Byte 'Buffer
            Dim datos_entrantes As New Paquete_Entrante
            Try
                'Recibe los datos
                Dim returnValue As Integer

                returnValue = ElSocket.ReceiveFrom(RecibirBytes, RecibirBytes.Length, SocketFlags.None, IPRecibida)

                If returnValue > 0 Then
                    ReDim Preserve RecibirBytes(returnValue - 1)
                End If

                LaIPRemota = CType(IPRecibida, IPEndPoint)
                Ip_Remoto = LaIPRemota.Address.ToString
                Puerto_Remoto = LaIPRemota.Port

                datos_entrantes.Ip = LaIPRemota.Address.ToString
                datos_entrantes.Puerto = LaIPRemota.Port
                datos_entrantes.Datos_Byte = RecibirBytes

                RaiseEvent Datos_Recibidos(datos_entrantes)

            Catch ex As SocketException
                Select Case ex.ErrorCode
                    Case 10054 '"Se ha forzado la interrupción de una conexión existente por el host remoto"
                        'RaiseEvent Datos_Recibidos(datos_entrantes)
                    Case 10040
                        RaiseEvent Datos_Recibidos(datos_entrantes)
                    Case Else
                        RaiseEvent Error_Socket("Recibir Datos " & ex.Message)
                End Select      
            End Try
        Loop

    End Sub
End Class
