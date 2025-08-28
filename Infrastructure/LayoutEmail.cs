using System.Text;

namespace AlertaBoletaService.Infrastructure
{
    public class LayoutEmail
    {
        private string _sistema = "AlertaBoleta";
        private string _assunto;
        private string _mensagem;

        public LayoutEmail(string assunto, string mensagem)
        {
            _assunto = assunto;
            _mensagem = mensagem;
        }

        public LayoutEmail(string sistema, string assunto, string mensagem)
        {
            _assunto = assunto;
            _sistema = sistema;
            _mensagem = mensagem;
        }

        public override string ToString()
        {
            return Layout();
        }

        private string Layout()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<html language='pt-BR'>");
            sb.Append("<head>");
            sb.Append("	<title></title>");
            sb.Append("	<meta http-equiv='Content-Type' content='text/html;charset=UTF-8' />");
            sb.Append("	<style type='text/css'>");
            sb.Append("		* {");
            sb.Append("			padding: 0;");
            sb.Append("			margin: 0;");
            sb.Append("    		border: 0;");
            sb.Append("    		font-family: Arial, sans-serif;");
            sb.Append("		}");
            sb.Append("		.topo{");
            sb.Append("			width: 100%;");
            sb.Append("			padding: 20px 0;");
            sb.Append("			text-align: center;");
            sb.Append("			font-size: 24px;");
            sb.Append("			font-weight: bold;");
            sb.Append("		}");
            sb.Append("		.footer{");
            sb.Append("			width: 100%;");
            sb.Append("    		margin-top: 30px;");
            sb.Append("    		padding: 15px;");
            sb.Append("    		background-color: #808080;");
            sb.Append("    		font-size: 12px;");
            sb.Append("    		text-align: center;");
            sb.Append("    		color: #FFFFFF;");
            sb.Append("		}");
            sb.Append("		.assunto{");
            sb.Append("			border-bottom: 3px solid #E3E3E3;");
            sb.Append("			margin: 20px;");
            sb.Append("			padding-bottom: 10px;");
            sb.Append("			font-size: 20px;");
            sb.Append("			font-weight: bold;");
            sb.Append("			color: #333;");
            sb.Append("		}");
            sb.Append("		.mensagem{");
            sb.Append("			margin: 20px;");
            sb.Append("			font-size: 14px;");
            sb.Append("			line-height: 1.6;");
            sb.Append("			color: #333;");
            sb.Append("		}");
            sb.Append("	</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("	<div class='topo'>COMEX SYSTEM - SHORT CONFIRMATION ALERTS</div>");
            sb.Append("	<div class='assunto'>" + _assunto + "</div>");
            sb.Append("	<div class='mensagem'>" + _mensagem + "</div>");
            sb.Append("	<div class='footer'>Email sent automatically by " + _sistema + " on " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "</div>");
            sb.Append("</body>");
            sb.Append("</html>");
            return sb.ToString();
        }

        public static implicit operator string(LayoutEmail layout)
        {
            return layout.ToString();
        }
    }
} 