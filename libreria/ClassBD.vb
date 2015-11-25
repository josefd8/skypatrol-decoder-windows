Imports System.Data.OleDb
Imports System.Data.Odbc
Imports System.Data.SQLite
Imports System.Data.SqlClient
Imports System.Xml
Imports System.IO

''' <summary>
''' Clase para el manejo de Base de datos Acces/ODBC
''' </summary>
''' <remarks></remarks>
Public Class BaseDatos

    Dim WithEvents WinSockSQL As New libreria.ClassUDP

    Private Declare Function SQLDataSources Lib "ODBC32.DLL" (ByVal henv As Integer, ByVal fDirection As Short, ByVal szDSN As String, ByVal cbDSNMax As Short, ByRef pcbDSN As Short, ByVal ByValszDescription As String, ByVal cbDescriptionMax As Short, ByRef pcbDescription As Short) As Short
    Private Declare Function SQLAllocEnv Lib "ODBC32.DLL" (ByRef env As Integer) As Short
    Private Declare Function SQLConfigDataSource Lib "ODBCCP32.DLL" (ByVal hwndParent As Integer, ByVal ByValfRequest As Integer, ByVal lpszDriver As String, ByVal lpszAttributes As String) As Integer
    Const SQL_SUCCESS As Integer = 0
    Const SQL_FETCH_NEXT As Integer = 1
    Private Const ODBC_ADD_DSN As Short = 1 ' Add user data source
    Private Const ODBC_CONFIG_DSN As Short = 2 ' Configure (edit) data source
    Private Const ODBC_REMOVE_DSN As Short = 3 ' Remove data source
    Private Const ODBC_ADD_SYS_DSN As Short = 4 'Add system data source
    Private Const vbAPINull As Integer = 0 ' NULL Pointer
    Private tipo_conexion As TipoConexion

    Public ConexionAcces As OleDbConnection
    Public ConexionODBC As OdbcConnection
    Public ConexionSQLite As SQLite.SQLiteConnection
    Public ConexionSQL_SERVER As SqlClient.SqlConnection
    Public SQL_SERVER_Datos As SqlClient.SqlDataReader
    Public AccesDataSet As Data.DataSet
    Public AccesDataAdapter As OleDbDataAdapter
    Public AccesDatos As OleDbDataReader
    Public SQLiteDatos As SQLiteDataReader
    Public ODBCDatos As OdbcDataReader
    Public Estado As Boolean
    Public CadenaError As String
    Public EstadoSQL As Boolean
    Public ValorSQL As Object

    ''' <summary>
    ''' Estructura Para definir los datos de coordenadas
    ''' </summary>
    ''' <remarks></remarks>
    Structure Coordenada
        Public Cardinalidad As String
        Public Coordenada As Double
    End Structure

    ''' <summary>
    ''' Estructura Para definir los datos de mensaje de los Equipos
    ''' </summary>
    ''' <remarks></remarks>
    Structure Registro
        Public Id_Vehiculo As String
        Public Latitud As Coordenada
        Public Longitud As Coordenada
        Public Altitud As Integer
        Public Velocidad As Integer
        Public Direccion As Integer
        Public Num_Evento As Integer
        Public Nro_Satelites As Integer
        Public Fecha As String
        Public Fecha_date As Date
        Public Fecha_Envio As Date
        Public Descripcion_Evento As String
        Public Data As String
        Public Data_Byte() As Byte
        Public In_Puertos As String
        Public Out_Puertos As String
        Public Erroneo As Boolean
        Public Valido As Boolean
        Public RawData As String
        Public Razon_Error As Integer
        Public Tipo_Mensaje As Integer
        Public AD1 As Double
        Public AD2 As Double
        Public Encendido As Object
        Public GPS_Valido As Integer
        Public Odometro As Long
        Public RTC As Date
        Public IP_Origen As String
        Public Puerto_Origen As String
    End Structure

    ''' <summary>
    ''' Indica las posibles acciones sobre una BD
    ''' </summary>
    ''' <remarks></remarks>
    Enum AccionSQL
        Query
        Insert
        Update
        Delete
    End Enum

    ''' <summary>
    ''' Indica el tipo de conexion a la BD
    ''' </summary>
    ''' <remarks></remarks>
    Enum TipoConexion
        Acces
        ODBC
        SQLite
    End Enum


    ''' <summary>
    ''' Cierra la conexion con la BD
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub cerrar_conexion()
        Try
            Select Case tipo_conexion
                Case TipoConexion.Acces
                    ConexionAcces.Close()
                Case TipoConexion.ODBC
                    ConexionODBC.Close()
                Case TipoConexion.SQLite
                    ConexionSQLite.Close()
            End Select
        Catch ex As Exception

        End Try
    End Sub

    ''' <summary>
    ''' Crea la conexión con una base de datos Acces (*.mdb)
    ''' </summary>
    ''' <param name="ArchivoAcces">Cadena Ruta del archivo de Acces (Path completa)</param>
    ''' <remarks></remarks>
    Public Sub Conexion(ByVal ArchivoAcces As String)
        tipo_conexion = TipoConexion.Acces
        Dim CadenaConexion As String
        CadenaConexion = "Provider=Microsoft.Jet.OLEDB.4.0; Data Source=" & ArchivoAcces
        Try
            Me.ConexionAcces = New OleDbConnection(CadenaConexion)
            Me.ConexionAcces.Open()
            Me.Estado = True
        Catch ex As Exception
            Me.Estado = False
            CadenaError = ex.Message
        End Try
    End Sub

    ''' <summary>
    ''' Crea la conexión con una base de datos Acces (*.mdb)
    ''' </summary>
    ''' <param name="Archivo">Cadena Ruta del archivo de Acces (Path completa)</param>
    ''' <remarks></remarks>
    Public Sub ConexionLite(ByVal Archivo As String)
        tipo_conexion = TipoConexion.SQLite
        Dim CadenaConexion As String
        CadenaConexion = "Data Source=" & Archivo & ";"

        Try
            Me.ConexionSQLite = New SQLite.SQLiteConnection(CadenaConexion)
            Me.ConexionSQLite.Open()
            If Me.ConexionSQLite.State = ConnectionState.Open Then
                Me.Estado = True
            End If
        Catch ex As Exception
            Me.Estado = False
            CadenaError = ex.Message
        End Try
    End Sub

    ''' <summary>
    ''' Crea la conexión con una base de datos ODBC (*.mdb)
    ''' </summary>
    ''' <param name="BDDSN">Nombre de la base de datos</param>
    ''' <param name="BDNombre">Nombre de la base de datos</param>
    ''' <param name="BDUser">Usuario</param>
    ''' <param name="BDPass">Contraseña</param>
    ''' <remarks></remarks>
    Public Sub Conexion(ByVal BDDSN As String, ByVal BDNombre As String, ByVal BDUser As String, ByVal BDPass As String)
        tipo_conexion = TipoConexion.ODBC
        Dim CadenaConexion As String

        CadenaConexion = "MSDASQL;DATABASE=" & BDNombre & ";DSN=" & BDDSN & ";UID=" & BDUser & ";PWD=" & BDPass
        'CadenaConexion = "Driver={MySQL ODBC 5.1 Driver};Server=" & "127.0.0.1" & ";Database=" & "dashboard" & ";User=" & "root" & ";Password=" & "" & ";Option=3;"

        Try

            Me.ConexionODBC = New OdbcConnection(CadenaConexion)
            Me.ConexionODBC.ConnectionTimeout = 60
            Me.ConexionODBC.Open()
            Me.Estado = True

        Catch ex As Exception
            Me.Estado = False
            Me.CadenaError = ex.Message.ToString
        End Try
    End Sub

    ''' <summary>
    ''' Genera una consulta a base de datos ole (mdb)
    ''' </summary>
    ''' <param name="CadenaSQL">Cadena SQL a ejecutar</param>
    ''' <param name="Conexion"></param>
    ''' <remarks></remarks>
    Public Sub OpenQuery(ByVal CadenaSQL As String, ByVal Conexion As OleDbConnection)
        Try
            Dim Comando As New OleDbCommand(CadenaSQL, Conexion)
            Me.AccesDatos = Comando.ExecuteReader()
            Me.Estado = True
        Catch ex As Exception
            Me.Estado = False
            Me.CadenaError = ex.Message
        End Try
    End Sub

    ''' <summary>
    ''' ejecuta la instruccion sql indicada en mdb
    ''' </summary>
    ''' <param name="CadenaSQL">Cadena SQL a ejecutar</param>
    ''' <param name="Conexion"></param>
    ''' <param name="Accion"></param>
    ''' <remarks></remarks>
    Public Sub ExecSQL(ByVal CadenaSQL As String, ByVal Conexion As OleDbConnection, ByVal Accion As AccionSQL)
        If Accion = AccionSQL.Query Then
            Try
                Dim Comando As New OleDbCommand(CadenaSQL, Conexion)
                Me.AccesDatos = Comando.ExecuteReader()
                Me.Estado = True
            Catch ex As Exception
                Me.CadenaError = ex.Message
                Me.Estado = False
            End Try
        Else
            Try
                Dim Comando As New OleDbCommand(CadenaSQL, Conexion)
                Comando.ExecuteNonQuery()
                Me.Estado = True
            Catch ex As Exception
                Me.CadenaError = ex.Message
                Me.Estado = False
            End Try
        End If
    End Sub

    ''' <summary>
    ''' Ejecuta el SQL para conexion SQLlite
    ''' </summary>
    ''' <param name="CadenaSQL">Cadena a ejecutar</param>
    ''' <param name="Conexion">Conexion SQLlite</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function ExecSQLDataGrid(ByVal CadenaSQL As String, ByVal Conexion As SQLiteConnection) As DataTable
        Dim db As SQLiteDataAdapter
        Dim ds As New DataSet
        Dim dt As New DataTable
        db = New SQLiteDataAdapter(CadenaSQL, Conexion)
        ds.Reset()
        db.Fill(ds)
        dt = ds.Tables(0)
        Return dt
    End Function

    Public Sub ExecSQL(ByVal CadenaSQL As String, ByVal Conexion As SQLiteConnection, ByVal Accion As AccionSQL)
        If Accion = AccionSQL.Query Then
            Try
                Dim Comando As New SQLiteCommand(CadenaSQL, Conexion)
                Me.SQLiteDatos = Comando.ExecuteReader()
                Me.Estado = True
            Catch ex As Exception
                Me.CadenaError = ex.Message
                Me.Estado = False
            End Try
        Else
            Try
                Dim Comando As New SQLiteCommand(CadenaSQL, Conexion)
                Comando.ExecuteNonQuery()
                Me.Estado = True
            Catch ex As Exception
                Me.CadenaError = ex.Message
                Me.Estado = False
            End Try
        End If
    End Sub

    ''' <summary>
    ''' ejecuta la instruccion sql indicada en ODBC
    ''' </summary>
    ''' <param name="CadenaSQL">Cadena SQL a ejecutar</param>
    ''' <param name="Conexion"></param>
    ''' <param name="Accion"></param>
    ''' <remarks></remarks>
    Public Sub ExecSQL(ByVal CadenaSQL As String, ByVal Conexion As OdbcConnection, ByVal Accion As AccionSQL)
        If Accion = AccionSQL.Query Then
            Try
                Dim Comando As New OdbcCommand(CadenaSQL, Conexion)
                Me.ODBCDatos = Comando.ExecuteReader()
                Me.EstadoSQL = True
            Catch ex As Exception
                Me.CadenaError = ex.Message
                Me.EstadoSQL = False
            End Try
        Else
            Try
                Dim Comando As New OdbcCommand(CadenaSQL, Conexion)
                'Me.ValorSQL = Comando.ExecuteNonQuery
                Me.ValorSQL = Comando.ExecuteScalar
                Me.EstadoSQL = True
            Catch ex As Exception
                Me.CadenaError = ex.Message
                Me.EstadoSQL = False
            End Try
        End If
    End Sub

    Public Sub Conexion_SQL_SERVER(ByVal Server As String, ByVal BD As String, ByVal User As String, ByVal Pass As String, ByVal Timeout As String)
        Dim CadenaConexion As String
        CadenaConexion = "Data Source=" & Server & ";" & _
                      "Initial Catalog=" & BD & ";" & _
              "User id=" & User & ";" & _
              "Password=" & Pass & ";Connection Timeout=" & Timeout & ";"
        Try
            If Not Me.ConexionSQL_SERVER Is Nothing Then
                Me.ConexionSQL_SERVER = Nothing
            End If
            Me.ConexionSQL_SERVER = New SqlClient.SqlConnection
            Me.ConexionSQL_SERVER.ConnectionString = CadenaConexion
            Me.ConexionSQL_SERVER.Open()
            Me.Estado = True
        Catch ex As Exception
            Me.Estado = False
            CadenaError = ex.Message
        End Try
    End Sub

    Public Sub ExecSQL_SERVER(ByVal CadenaSQL As String, ByVal Conexion As SqlClient.SqlConnection, ByVal Accion As AccionSQL)
        If Accion = AccionSQL.Query Then
            Try
                Dim Comando As New SqlClient.SqlCommand(CadenaSQL, Conexion)
                Me.SQL_SERVER_Datos = Comando.ExecuteReader()
                Me.EstadoSQL = True
            Catch ex As Exception
                Me.CadenaError = ex.Message
                Me.EstadoSQL = False
            End Try
        Else
            Try
                Dim Comando As New SqlClient.SqlCommand(CadenaSQL, Conexion)
                'Comando.CommandTimeout = 30
                Me.ValorSQL = Comando.ExecuteScalar
                Me.EstadoSQL = True
            Catch ex As Exception
                Me.CadenaError = ex.Message
                Me.EstadoSQL = False
            End Try
        End If
    End Sub

End Class
