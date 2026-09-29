using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Globalization;
using System.Reflection;

namespace Lanxi {
public class Node {
 [Browsable(false)] public int Id {get;set;}
 [DisplayName("节点名称")] public string Name {get;set;}
 [Browsable(false)] public float X {get;set;}
 [Browsable(false)] public float Y {get;set;}
 [DisplayName("恒压源"),Description("每个连通网络至少设置一个恒压源；源节点保持初始压力。")] public bool Source {get;set;}
 [DisplayName("压力与振动测点")] public bool Sensor {get;set;}
 public override string ToString(){return Name;}
}
public class Pipe {
 [Browsable(false)] public int Id {get;set;}
 [Browsable(false)] public int A {get;set;}
 [Browsable(false)] public int B {get;set;}
 [DisplayName("管段名称")] public string Name {get;set;}
 [DisplayName("长度 m"),Description("计算使用此长度，与画布线段长度无关。范围 10—2000 m。")] public double Length {get;set;}
 [DisplayName("内径 mm")] public double Diameter {get;set;}
 [DisplayName("压力波速 m/s")] public double Speed {get;set;}
 [DisplayName("阻尼系数 1/s")] public double Loss {get;set;}
 [DisplayName("启用漏点")] public bool Leak {get;set;}
 [DisplayName("漏点位置 %"),Description("沿起点到终点方向，范围 5—95%。")] public double Position {get;set;}
 [DisplayName("漏孔直径 mm")] public double Hole {get;set;}
 public override string ToString(){return Name+"  N"+A+" → N"+B;}
}
public class Settings {
 [DisplayName("初始表压 MPa")] public double Pressure {get;set;}
 [DisplayName("计算时长 s")] public double Duration {get;set;}
 [DisplayName("泄漏开始 s")] public double Start {get;set;}
 [DisplayName("开启历时 s")] public double Rise {get;set;}
 [DisplayName("采样频率 Hz")] public int SampleRate {get;set;}
 [DisplayName("测量噪声 MPa")] public double Noise {get;set;}
 [DisplayName("振动固有频率 Hz")] public double VibrationFrequency {get;set;}
 [DisplayName("振动阻尼比")] public double VibrationDamping {get;set;}
 [DisplayName("压力响应系数"),Description("等效加速度激励系数，单位 (m/s²)/MPa；用于场景演示。")] public double VibrationGain {get;set;}
 [DisplayName("振动噪声 m/s²")] public double VibrationNoise {get;set;}
 [DisplayName("随机种子")] public int Seed {get;set;}
 public Settings(){Pressure=.6; Duration=4;Start=1;Rise=.04;SampleRate=500;Noise=.0002;VibrationFrequency=20;VibrationDamping=.10;VibrationGain=30;VibrationNoise=.002;Seed=42;}
}
public class Project {
 public string Format {get;set;}
 public List<Node> Nodes {get;set;}
 public List<Pipe> Pipes {get;set;}
 public Settings Settings {get;set;}
 public Project(){Format="LANXI-1";Nodes=new List<Node>();Pipes=new List<Pipe>();Settings=new Settings();}
 public static Project Demo(int type){
  Project p=new Project();
  p.Nodes.Add(new Node{Id=1,Name="N1 恒压源",X=95,Y=180,Source=true});
  p.Nodes.Add(new Node{Id=2,Name="N2",X=280,Y=180,Sensor=true});
  p.Nodes.Add(new Node{Id=3,Name="N3",X=500,Y=100,Sensor=true});
  p.Nodes.Add(new Node{Id=4,Name="N4",X=500,Y=300,Sensor=true});
  p.Pipes.Add(NewPipe(1,1,2));p.Pipes.Add(NewPipe(2,2,3));p.Pipes.Add(NewPipe(3,type==0?3:2,4));
  if(type==0){p.Nodes[2].X=430;p.Nodes[2].Y=180;p.Nodes[3].X=610;p.Nodes[3].Y=180;}
  if(type==2)p.Pipes.Add(NewPipe(4,3,4));
  p.Pipes[1].Leak=true;p.Pipes[1].Position=55;p.Pipes[1].Hole=8;
  return p;
 }
 public static Pipe NewPipe(int id,int a,int b){return new Pipe{Id=id,Name="P"+id,A=a,B=b,Length=100,Diameter=100,Speed=1000,Loss=.15,Position=50,Hole=8};}
 public static string Validate(Project p){
  if(p==null||p.Format!="LANXI-1"||p.Nodes==null||p.Pipes==null||p.Settings==null)return "工程格式不受支持。";
  if(p.Nodes.Count<2||p.Nodes.Count>40||p.Pipes.Count<1||p.Pipes.Count>60)return "请设置 2—40 个节点、1—60 条管段。";
  if(p.Nodes.Any(n=>n==null||!Finite(n.X)||!Finite(n.Y)||n.X<20||n.X>10000||n.Y<20||n.Y>10000||string.IsNullOrWhiteSpace(n.Name)||n.Name.Length>40))return "节点名称或坐标无效。";
  if(p.Nodes.Select(n=>n.Id).Distinct().Count()!=p.Nodes.Count)return "节点编号重复。";
  if(!p.Nodes.Any(n=>n.Source))return "至少设置一个恒压源。";
  if(!p.Nodes.Any(n=>n.Sensor&&!n.Source))return "至少设置一个非恒压源的压力测点。";
  if(p.Pipes.Any(e=>e==null||!p.Nodes.Any(n=>n.Id==e.A)||!p.Nodes.Any(n=>n.Id==e.B)||e.A==e.B||!Range(e.Length,10,2000)||!Range(e.Diameter,20,1000)||!Range(e.Speed,200,1600)||!Range(e.Loss,0,5)||!Range(e.Position,5,95)||!Range(e.Hole,.1,30)||e.Hole>e.Diameter*.5||string.IsNullOrWhiteSpace(e.Name)||e.Name.Length>40))return "检查管段：长度10—2000m，内径20—1000mm，波速200—1600m/s，阻尼0—5，位置5—95%，漏孔0.1—30mm且不超过内径的一半。";
  if(p.Pipes.Select(e=>e.Id).Distinct().Count()!=p.Pipes.Count)return "管段编号重复。";
  Settings s=p.Settings;
  if(!Range(s.Pressure,.05,2)||!Range(s.Duration,.5,20)||!Range(s.Start,.1,s.Duration-.1)||!Range(s.Rise,.001,1)||s.Start+s.Rise>=s.Duration||s.SampleRate<50||s.SampleRate>2000||!Range(s.Noise,0,.01))return "检查计算设置：压力0.05—2MPa，时长0.5—20s，采样50—2000Hz，漏点开启须在结束前完成，噪声0—0.01MPa。";
  if(!Range(s.VibrationFrequency,3,80)||!Range(s.VibrationDamping,.03,1)||!Range(s.VibrationGain,.1,200)||!Range(s.VibrationNoise,0,.1)||s.SampleRate<10*s.VibrationFrequency)return "振动设置：固有频率3—80Hz，阻尼比0.03—1，响应系数0.1—200，噪声0—0.1m/s²；采样频率至少为固有频率的10倍。";
  HashSet<int> seen=new HashSet<int>(p.Nodes.Where(n=>n.Source).Select(n=>n.Id));
  for(int k=0;k<p.Nodes.Count;k++)foreach(Pipe e in p.Pipes)if(seen.Contains(e.A)||seen.Contains(e.B)){seen.Add(e.A);seen.Add(e.B);}
  if(seen.Count!=p.Nodes.Count)return "存在未连接恒压源的节点，请补充连管或删除孤立节点。";
  return null;
 }
 static bool Finite(double x){return !double.IsNaN(x)&&!double.IsInfinity(x);}
 static bool Range(double x,double lo,double hi){return Finite(x)&&x>=lo&&x<=hi;}
}
public class Result {
 public double[] Time;
 public List<double[]> Values=new List<double[]>();
 public List<string> Names=new List<string>();
 public List<double[]> Vibration=new List<double[]>();
 public List<bool> DetectionChannel=new List<bool>();
 public double Dt; public int Steps; public double Min;
}
public class Oscillator {
 public double U,V,A; readonly double K,C;
 public Oscillator(double frequency,double damping){double w=2*Math.PI*frequency;K=w*w;C=2*damping*w;}
 public double Step(double force,double dt){double up=U+dt*V+.25*dt*dt*A;double vp=V+.5*dt*A;A=(force-C*vp-K*up)/(1+.5*C*dt+.25*K*dt*dt);U=up+.25*dt*dt*A;V=vp+.5*dt*A;return A;}
}
class Edge {
 public int A,B,N;public double Z,Speed,Dx,Loss;public double[] P,Q,NP,NQ;
 public Edge(int a,int b,double length,Pipe e,double p0){A=a;B=b;N=Math.Max(3,(int)Math.Ceiling(length/8));Speed=e.Speed;Dx=length/N;Z=998*e.Speed/(Math.PI*Math.Pow(e.Diameter/1000,2)/4);Loss=e.Loss;P=Enumerable.Repeat(p0,N+1).ToArray();Q=new double[N+1];NP=new double[N+1];NQ=new double[N+1];}
}
public static class Engine {
 public static Result Run(Project p,double mesh=8){
  string err=Project.Validate(p);if(err!=null)throw new Exception(err);
  Settings s=p.Settings;double p0=s.Pressure*1e6;
  List<Node> nodes=new List<Node>(p.Nodes);List<Edge> es=new List<Edge>();Dictionary<int,double> holes=new Dictionary<int,double>();
  Func<int,int> ix=id=>nodes.FindIndex(n=>n.Id==id);
  foreach(Pipe e in p.Pipes){int a=ix(e.A),b=ix(e.B);if(e.Leak){int j=nodes.Count;nodes.Add(new Node{Id=-j-1,Name="漏点"});holes[j]=.62*Math.PI*Math.Pow(e.Hole/1000,2)/4*Math.Sqrt(2/998.0);es.Add(new Edge(a,j,e.Length*e.Position/100,e,p0));es.Add(new Edge(j,b,e.Length*(1-e.Position/100),e,p0));}else es.Add(new Edge(a,b,e.Length,e,p0));}
  // Grid spacing parameter is used only by the convergence verification.
  foreach(Edge e in es){double length=e.Dx*e.N;e.N=Math.Max(3,(int)Math.Ceiling(length/mesh));e.Dx=length/e.N;e.P=Enumerable.Repeat(p0,e.N+1).ToArray();e.Q=new double[e.N+1];e.NP=new double[e.N+1];e.NQ=new double[e.N+1];}
  double dt=Math.Min(1.0/(40*s.VibrationFrequency),Math.Min(1.0/s.SampleRate,es.Min(e=>e.Dx/e.Speed)*.8));
  int steps=(int)Math.Ceiling(s.Duration/dt);dt=s.Duration/steps;
  if(steps>300000||steps*(long)es.Sum(e=>e.N)>180000000)throw new Exception("当前模型计算量过大，请缩短计算时长、增大短管段长度或减少管段。");
  int count=(int)Math.Floor(s.Duration*s.SampleRate)+1;Result r=new Result{Time=new double[count],Dt=dt,Steps=steps,Min=p0};
  List<int> obs=new List<int>();foreach(Node n in p.Nodes.Where(n=>n.Sensor)){obs.Add(ix(n.Id));r.Names.Add(n.Name);r.Values.Add(new double[count]);r.Vibration.Add(new double[count]);r.DetectionChannel.Add(!n.Source);}
  double[] np=Enumerable.Repeat(p0,nodes.Count).ToArray(),old=(double[])np.Clone();
  Random rng=new Random(s.Seed),vrng=new Random(unchecked(s.Seed+7919));int outi=0;
  Oscillator[] oscillators=obs.Select(j=>new Oscillator(s.VibrationFrequency,s.VibrationDamping)).ToArray();double[] va=new double[obs.Count],vold=new double[obs.Count];
  Action<double[],double[],double,double> record=(prev,curr,tprev,tcur)=>{while(outi<count&&outi/(double)s.SampleRate<=tcur+1e-10){double t=outi/(double)s.SampleRate;r.Time[outi]=t;double f=tcur>tprev?(t-tprev)/(tcur-tprev):0;f=Math.Max(0,Math.Min(1,f));for(int j=0;j<obs.Count;j++){double value=prev[obs[j]]*(1-f)+curr[obs[j]]*f;r.Values[j][outi]=value/1e6+s.Noise*(rng.NextDouble()+rng.NextDouble()+rng.NextDouble()+rng.NextDouble()+rng.NextDouble()+rng.NextDouble()-3)*Math.Sqrt(2);r.Vibration[j][outi]=vold[j]*(1-f)+va[j]*f+s.VibrationNoise*(vrng.NextDouble()+vrng.NextDouble()+vrng.NextDouble()+vrng.NextDouble()+vrng.NextDouble()+vrng.NextDouble()-3)*Math.Sqrt(2);}outi++;}};
  record(np,np,0,0);
  double[] incomingA=new double[es.Count],incomingB=new double[es.Count],sum=new double[nodes.Count],admit=new double[nodes.Count];
  for(int step=1;step<=steps;step++){
   double time=step*dt;Array.Clear(sum,0,sum.Length);Array.Clear(admit,0,admit.Length);Array.Copy(np,old,np.Length);
   for(int k=0;k<es.Count;k++){
    Edge e=es[k];double f=e.Speed*dt/e.Dx;int n=e.N;double damp=Math.Exp(-e.Loss*dt);
    double ca=(1-f)*(e.P[0]-e.Z*e.Q[0]*damp)+f*(e.P[1]-e.Z*e.Q[1]*damp);
    double cb=(1-f)*(e.P[n]+e.Z*e.Q[n]*damp)+f*(e.P[n-1]+e.Z*e.Q[n-1]*damp);
    incomingA[k]=ca;incomingB[k]=cb;sum[e.A]+=ca/e.Z;sum[e.B]+=cb/e.Z;admit[e.A]+=1/e.Z;admit[e.B]+=1/e.Z;
    for(int j=1;j<n;j++){double cp=(1-f)*(e.P[j]+e.Z*e.Q[j]*damp)+f*(e.P[j-1]+e.Z*e.Q[j-1]*damp);double cm=(1-f)*(e.P[j]-e.Z*e.Q[j]*damp)+f*(e.P[j+1]-e.Z*e.Q[j+1]*damp);e.NP[j]=(cp+cm)/2;e.NQ[j]=(cp-cm)/(2*e.Z);}
   }
   for(int j=0;j<nodes.Count;j++){
    if(nodes[j].Source){np[j]=p0;continue;}
    double coeff=holes.ContainsKey(j)?holes[j]*Math.Max(0,Math.Min(1,(time-s.Start)/s.Rise)):0;
    double h=sum[j]/admit[j];if(h<=0)throw new Exception("计算出现非正表压，请减小漏孔或调整管网参数。");
    double b=coeff/admit[j];double root=2*h/(Math.Sqrt(b*b+4*h)+b);np[j]=root*root;r.Min=Math.Min(r.Min,np[j]);
   }
   for(int k=0;k<es.Count;k++){Edge e=es[k];e.NP[0]=np[e.A];e.NQ[0]=(np[e.A]-incomingA[k])/e.Z;e.NP[e.N]=np[e.B];e.NQ[e.N]=(incomingB[k]-np[e.B])/e.Z;double[] t=e.P;e.P=e.NP;e.NP=t;t=e.Q;e.Q=e.NQ;e.NQ=t;if(e.P.Any(v=>double.IsNaN(v)||double.IsInfinity(v)||v<=0))throw new Exception("压力超出当前计算范围，请调整参数。");}
   for(int j=0;j<obs.Count;j++){vold[j]=va[j];va[j]=oscillators[j].Step(s.VibrationGain*(np[obs[j]]-p0)/1e6,dt);if(double.IsNaN(va[j])||double.IsInfinity(va[j]))throw new Exception("振动响应异常，请检查参数。");}
   record(old,np,(step-1)*dt,time);
  }
  return r;
 }
 public static void Csv(Result r,string file){StringBuilder b=new StringBuilder("time_s");foreach(string n in r.Names){string label=n.Replace("\"","\"\"");b.Append(",\"").Append(label).Append("_pressure_MPa\",").Append("\"").Append(label).Append("_vibration_m_s2\"");}b.AppendLine();for(int i=0;i<r.Time.Length;i++){b.Append(r.Time[i].ToString("G12",CultureInfo.InvariantCulture));for(int j=0;j<r.Names.Count;j++)b.Append(',').Append(r.Values[j][i].ToString("G12",CultureInfo.InvariantCulture)).Append(',').Append(r.Vibration[j][i].ToString("G12",CultureInfo.InvariantCulture));b.AppendLine();}File.WriteAllText(file,b.ToString(),new UTF8Encoding(true));}

}
public class Sample {public double[] X;public int Y;}
public class Model {
 public List<Sample> Samples=new List<Sample>();public int TrainCount;public string Version="LANXI-AI-2";public double[] Mean,Scale;
 public static double Rms(IEnumerable<double> values){return Math.Sqrt(values.Select(v=>v*v).Average());}
 public static double[] Features(Result r){List<double[]> all=new List<double[]>();for(int j=0;j<r.Values.Count;j++){if(!r.DetectionChannel[j])continue;double[] y=r.Values[j],v=r.Vibration[j];int n=y.Length;double basep=y.Take(Math.Max(3,n/10)).Average(),avg=y.Average();all.Add(new[]{(basep-y.Skip(n*3/4).Average())/basep,(y.Max()-y.Min())/basep,Math.Sqrt(y.Select(x=>(x-avg)*(x-avg)).Average())/basep,Rms(v),v.Max()-v.Min(),Rms(v.Skip(n/4))-Rms(v.Take(Math.Max(3,n/10)))});}if(all.Count==0)throw new Exception("缺少检测测点。");return Enumerable.Range(0,6).Select(k=>all.Average(a=>a[k])).ToArray();}
 public double Distance(double[] a,double[] b){double sum=0;for(int i=0;i<a.Length;i++)sum+=Math.Pow((a[i]-b[i])/Scale[i],2);return sum;}
 public int Vote(double[] x){return Samples.OrderBy(v=>Distance(v.X,x)).Take(7).Sum(v=>v.Y);}
 public string Predict(Result r){if(Samples.Count<7)return "AI 模型未加载";double[] x=Features(r);int votes=Vote(x);double dist=Math.Sqrt(Samples.Min(v=>Distance(v.X,x)));return "检测结果："+(votes>=4?"有泄漏":"无泄漏")+"\r\n输入：全部非源测点的压力与振动\r\n近邻参考票数："+Math.Max(votes,7-votes)+" / 7"+(dist>6?"\r\n波形偏离训练样本，请复核。":"\r\n票数不是正确概率，结论供仿真参考。")+"\r\n本功能不输出漏点位置。";}
 public static Model Load(){using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("lanxi.model")){if(stream==null)return null;using(StreamReader sr=new StreamReader(stream)){Model m=Json.Read<Model>(sr.ReadToEnd());return m.Version=="LANXI-AI-2"?m:null;}}}
 public static Model Train(string folder){
  Model m=new Model();Random rand=new Random(20260930);List<Sample> tests=new List<Sample>();
  for(int k=0;k<320;k++){
   Project p=Project.Demo(k%3);p.Settings.Pressure=.3+rand.NextDouble()*.6;p.Settings.Duration=3;p.Settings.Start=.4+rand.NextDouble()*.7;p.Settings.Rise=.02+rand.NextDouble()*.08;p.Settings.SampleRate=500;p.Settings.Noise=.0001+rand.NextDouble()*.001;p.Settings.Seed=rand.Next();p.Settings.VibrationFrequency=10+rand.NextDouble()*25;p.Settings.VibrationDamping=.06+rand.NextDouble()*.2;p.Settings.VibrationGain=15+rand.NextDouble()*35;p.Settings.VibrationNoise=.001+rand.NextDouble()*.006;
   foreach(Pipe e in p.Pipes){e.Length=50+rand.NextDouble()*150;e.Diameter=75+rand.NextDouble()*75;e.Speed=700+rand.NextDouble()*500;e.Loss=.05+rand.NextDouble()*.35;e.Leak=false;}
   int label=k%2;Pipe leak=p.Pipes[1];leak.Leak=label==1;leak.Hole=4+rand.NextDouble()*10;leak.Position=20+rand.NextDouble()*60;
   Sample sample=new Sample{X=Features(Engine.Run(p)),Y=label};if(k<240)m.Samples.Add(sample);else tests.Add(sample);
  }
  m.TrainCount=m.Samples.Count;m.Mean=Enumerable.Range(0,6).Select(i=>m.Samples.Average(v=>v.X[i])).ToArray();m.Scale=Enumerable.Range(0,6).Select(i=>Math.Max(1e-6,Math.Sqrt(m.Samples.Average(v=>Math.Pow(v.X[i]-m.Mean[i],2))))).ToArray();
  int tp=0,tn=0,fp=0,fn=0;foreach(Sample v in tests){bool hit=m.Vote(v.X)>=4;if(hit&&v.Y==1)tp++;else if(hit)fp++;else if(v.Y==1)fn++;else tn++;}
  File.WriteAllText(Path.Combine(folder,"AI模型.json"),Json.Write(m),Encoding.UTF8);
  File.WriteAllText(Path.Combine(folder,"AI模型验证.txt"),"澜析双通道泄漏二分类模型 V1.1\r\n训练事件240，留出事件80；每事件汇总全部非源测点的双通道特征。\r\n标准化仅使用训练事件。训练与留出均来自同一仿真机制，不能作为现场精度证明。\r\n固定随机种子20260930；真阳性="+tp+"，真阴性="+tn+"，假阳性="+fp+"，假阴性="+fn+"\r\nAI输入为压力和振动波形，不读取漏点参数或标签，不输出位置。\r\n6维统计特征；7近邻标准化距离投票。\r\n振动采用本地压力驱动的等效二阶响应，Newmark平均加速度积分；参数是演示设置，未作现场标定。",Encoding.UTF8);return m;
 }
}
public static class Json {public static string Write(object o){return new JavaScriptSerializer{MaxJsonLength=16000000}.Serialize(o);}public static T Read<T>(string s){return new JavaScriptSerializer{MaxJsonLength=16000000}.Deserialize<T>(s);}}
class Canvas:Panel {public Canvas(){DoubleBuffered=true;ResizeRedraw=true;}}
public class MainForm:Form {
 Project project=Project.Demo(1);Result result;Model model;object selected;Node linkStart,drag;string mode="选择";bool busy,dirty;PropertyGrid props;Canvas canvas;Chart chart;Label hint,ai,status;ComboBox sensor;Panel editor;FlowLayoutPanel toolbar;TabControl tabs;
 Color blue=Color.FromArgb(24,91,132),teal=Color.FromArgb(0,142,155);string root;
 public MainForm(){
  Text="澜析管网泄漏检测系统 1.1";Font=new Font("Microsoft YaHei UI",9);BackColor=Color.FromArgb(243,246,250);Size=new Size(1330,870);MinimumSize=new Size(1080,720);StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.Dpi;
  root=Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).FullName;model=Model.Load();
  Panel banner=new Panel{Dock=DockStyle.Top,Height=68,BackColor=blue};banner.Controls.Add(new Label{Text="澜析  /  管网泄漏检测系统",ForeColor=Color.White,Font=new Font(Font.FontFamily,20,FontStyle.Bold),AutoSize=true,Location=new Point(20,11)});banner.Controls.Add(new Label{Text="压力与振动   ·   泄漏判断",ForeColor=Color.FromArgb(191,220,237),AutoSize=true,Location=new Point(550,28)});
  toolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=84,Padding=new Padding(12,6,12,6),BackColor=Color.White,WrapContents=true};
  AddButton("新建",()=>New());AddButton("打开工程",()=>Open());AddButton("保存工程",()=>Save());AddButton("直管示例",()=>Demo(0));AddButton("支路示例",()=>Demo(1));AddButton("环网示例",()=>Demo(2));AddButton("选择/移动",()=>Mode("选择"));AddButton("添加节点",()=>Mode("节点"));AddButton("连接管段",()=>Mode("连接"));AddButton("设置漏点",()=>Mode("漏点"));AddButton("删除选中",()=>Delete());AddButton("计算设置",()=>{selected=project.Settings;props.SelectedObject=selected;});AddButton("运行仿真",async()=>await Run());AddButton("导出 CSV",()=>ExportCsv());AddButton("导出波形图",()=>ExportPng());AddButton("操作说明",()=>Help());
  status=new Label{Dock=DockStyle.Bottom,Height=30,Padding=new Padding(14,6,0,0),Text="就绪  |  离线计算  |  压力 MPa  |  振动 m/s²",BackColor=Color.FromArgb(229,237,244)};
  SplitContainer split=new SplitContainer{Dock=DockStyle.Fill,FixedPanel=FixedPanel.Panel2,SplitterWidth=6};split.Panel1MinSize=550;split.Size=new Size(1250,660);split.SplitterDistance=935;split.Panel2MinSize=285;
  tabs=new TabControl{Dock=DockStyle.Fill};TabPage design=new TabPage("  管网工作台  "),wave=new TabPage("  压力与振动  ");tabs.TabPages.Add(design);tabs.TabPages.Add(wave);
  hint=new Label{Dock=DockStyle.Bottom,Height=42,Padding=new Padding(12,7,0,0),Text="选择节点或管段，在右侧修改属性。箭头表示管段起点到终点方向。",BackColor=Color.White};
  editor=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.White};canvas=new Canvas{Location=new Point(0,0),Size=new Size(1000,650),BackColor=Color.FromArgb(249,251,253)};editor.Controls.Add(canvas);design.Controls.Add(editor);design.Controls.Add(hint);canvas.Paint+=PaintNetwork;canvas.MouseDown+=MouseDownNet;canvas.MouseMove+=MouseMoveNet;canvas.MouseUp+=(s,e)=>{drag=null;};
  chart=new Chart{Dock=DockStyle.Fill,BackColor=Color.White};ChartArea ca=new ChartArea("压力");ca.AxisX.Title="时间 / s";ca.AxisY.Title="压力 / MPa";ca.AxisX.MajorGrid.LineColor=Color.FromArgb(231,236,243);ca.AxisY.MajorGrid.LineColor=Color.FromArgb(231,236,243);ca.AxisY.IsStartedFromZero=false;ca.AxisY.LabelStyle.Format="0.000";ca.AxisX.LabelStyle.Format="0.0";ca.Position=new ElementPosition(3,12,94,36);chart.ChartAreas.Add(ca);ChartArea vib=new ChartArea("振动");vib.Position=new ElementPosition(3,56,94,37);vib.AxisX.Title="时间 / s";vib.AxisY.Title="振动加速度 / m/s²";vib.AxisY.LabelStyle.Format="0.000";vib.AxisX.LabelStyle.Format="0.0";vib.AxisX.MajorGrid.LineColor=Color.FromArgb(231,236,243);vib.AxisY.MajorGrid.LineColor=Color.FromArgb(231,236,243);chart.ChartAreas.Add(vib);chart.Legends.Add(new Legend{Docking=Docking.Top,Font=Font});chart.Titles.Add("压力与振动时程");wave.Controls.Add(chart);wave.Controls.Add(new Label{Dock=DockStyle.Bottom,Height=36,Text="  上图压力，下图振动；检测结果仅回答是否泄漏。CSV 包含两个通道。",Padding=new Padding(4,9,0,0)});
  split.Panel1.Controls.Add(tabs);
  TableLayoutPanel side=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,Padding=new Padding(5)};side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));side.RowStyles.Add(new RowStyle(SizeType.Absolute,34));side.RowStyles.Add(new RowStyle(SizeType.Percent,100));side.RowStyles.Add(new RowStyle(SizeType.Absolute,36));side.RowStyles.Add(new RowStyle(SizeType.Absolute,35));side.RowStyles.Add(new RowStyle(SizeType.Absolute,145));side.RowStyles.Add(new RowStyle(SizeType.Absolute,44));
  side.Controls.Add(new Label{Text="对象属性",Font=new Font(Font.FontFamily,12,FontStyle.Bold),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);
  props=new PropertyGrid{Dock=DockStyle.Fill,ToolbarVisible=false,HelpVisible=true,PropertySort=PropertySort.NoSort,SelectedObject=project.Settings};props.PropertyValueChanged+=(s,e)=>{Changed();CanvasSize();canvas.Invalidate();};side.Controls.Add(props,0,1);
  side.Controls.Add(new Label{Text="AI 泄漏判断",Font=new Font(Font.FontFamily,12,FontStyle.Bold),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,2);
  sensor=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,DrawMode=DrawMode.OwnerDrawFixed};sensor.DrawItem+=(s,e)=>{e.DrawBackground();if(e.Index>=0)TextRenderer.DrawText(e.Graphics,sensor.Items[e.Index].ToString(),Font,e.Bounds,e.ForeColor,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);e.DrawFocusRectangle();};sensor.SelectedIndexChanged+=(s,e)=>UpdateAi();side.Controls.Add(sensor,0,3);
  ai=new Label{Dock=DockStyle.Fill,Text="运行仿真后查看泄漏判断。",Padding=new Padding(7),BackColor=Color.White,ForeColor=blue};side.Controls.Add(ai,0,4);
  side.Controls.Add(new Label{Dock=DockStyle.Fill,Text="测点下拉框用于突出显示曲线。\r\n检测结果针对整个仿真场景。",ForeColor=Color.DimGray},0,5);split.Panel2.Controls.Add(side);
  Controls.Add(split);Controls.Add(toolbar);Controls.Add(banner);Controls.Add(status);FormClosing+=(s,e)=>{if(busy){e.Cancel=true;return;}if(!ConfirmDiscard())e.Cancel=true;};
 }
 void AddButton(string text,Action action){Button b=new Button{Text=text,AutoSize=true,Height=30,FlatStyle=FlatStyle.Flat,BackColor=Color.White,Margin=new Padding(3),Padding=new Padding(4,0,4,0)};b.FlatAppearance.BorderColor=Color.FromArgb(215,225,235);b.Click+=(s,e)=>{try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,"操作提示",MessageBoxButtons.OK,MessageBoxIcon.Information);}};toolbar.Controls.Add(b);}
 void Mode(string m){mode=m;linkStart=null;hint.Text=m=="连接"?"依次点击两个节点建立管段；物理长度在右侧属性中设置。":m=="节点"?"在空白处单击添加节点；首个节点为恒压源，其余节点默认设为测点。":m=="漏点"?"点击管段添加或移动漏点；在右侧可修改位置、孔径，或关闭“启用漏点”。":"点击对象修改属性；按住节点拖动。";tabs.SelectedIndex=0;canvas.Invalidate();}
 bool ConfirmDiscard(){return !dirty||MessageBox.Show(this,"当前工程有未保存修改，是否放弃这些修改？","保存提醒",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes;}
 void New(){if(!ConfirmDiscard())return;project=new Project();Reset();Mode("节点");}
 void Demo(int type){if(!ConfirmDiscard())return;project=Project.Demo(type);Reset();}
 void Reset(){selected=null;drag=null;linkStart=null;mode="选择";props.SelectedObject=project.Settings;Changed();dirty=false;CanvasSize();canvas.Invalidate();tabs.SelectedIndex=0;}
 void Changed(){dirty=true;result=null;chart.Series.Clear();sensor.Items.Clear();ai.Text="参数已更新，请重新运行仿真。";status.Text="工程已修改  |  重新仿真后可导出结果";}
 void CanvasSize(){canvas.Size=new Size(Math.Max(1000,(int)project.Nodes.Select(n=>n.X).DefaultIfEmpty(900).Max()+100),Math.Max(650,(int)project.Nodes.Select(n=>n.Y).DefaultIfEmpty(550).Max()+100));}
 void Open(){if(!ConfirmDiscard())return;using(OpenFileDialog d=new OpenFileDialog{Filter="澜析工程 (*.json)|*.json",InitialDirectory=Folder("03_示例工程")})if(d.ShowDialog()==DialogResult.OK){if(new FileInfo(d.FileName).Length>2000000)throw new Exception("工程文件过大。");Project p=Json.Read<Project>(File.ReadAllText(d.FileName));string e=Project.Validate(p);if(e!=null)throw new Exception(e);project=p;Reset();status.Text="已打开："+Path.GetFileName(d.FileName);}}
 void Save(){string err=Project.Validate(project);if(err!=null)throw new Exception(err);using(SaveFileDialog d=new SaveFileDialog{Filter="澜析工程 (*.json)|*.json",FileName="我的管网.json",InitialDirectory=Folder("03_示例工程")})if(d.ShowDialog()==DialogResult.OK){File.WriteAllText(d.FileName,Json.Write(project),Encoding.UTF8);dirty=false;status.Text="工程已保存";}}
 string Folder(string sub){string f=Path.Combine(root,sub);return Directory.Exists(f)?f:Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);}
 void Delete(){Node n=selected as Node;Pipe p=selected as Pipe;if(n!=null){project.Pipes.RemoveAll(e=>e.A==n.Id||e.B==n.Id);project.Nodes.Remove(n);}else if(p!=null)project.Pipes.Remove(p);else return;selected=null;linkStart=null;props.SelectedObject=project.Settings;Changed();canvas.Invalidate();}
 void MouseDownNet(object sender,MouseEventArgs e){if(busy||e.Button!=MouseButtons.Left)return;Node n=project.Nodes.LastOrDefault(v=>Math.Pow(v.X-e.X,2)+Math.Pow(v.Y-e.Y,2)<225);
  if(mode=="节点"&&n==null){if(project.Nodes.Count>=40){MessageBox.Show("最多支持 40 个节点。");return;}int id=project.Nodes.Select(v=>v.Id).DefaultIfEmpty(0).Max()+1;n=new Node{Id=id,Name="N"+id,X=Math.Max(25,e.X),Y=Math.Max(25,e.Y),Source=project.Nodes.Count==0,Sensor=project.Nodes.Count>0};project.Nodes.Add(n);selected=n;props.SelectedObject=n;Changed();CanvasSize();}
  else if(mode=="连接"&&n!=null){if(linkStart==null)linkStart=n;else if(linkStart!=n){if(project.Pipes.Count>=60){MessageBox.Show("最多支持 60 条管段。");return;}if(project.Pipes.Any(v=>(v.A==linkStart.Id&&v.B==n.Id)||(v.B==linkStart.Id&&v.A==n.Id))){MessageBox.Show("这两个节点已有管段。");linkStart=null;return;}Pipe pipe=Project.NewPipe(project.Pipes.Select(v=>v.Id).DefaultIfEmpty(0).Max()+1,linkStart.Id,n.Id);project.Pipes.Add(pipe);selected=pipe;props.SelectedObject=pipe;linkStart=null;Changed();}}
  else if(n!=null){selected=n;props.SelectedObject=n;if(mode=="选择")drag=n;}
  else {Pipe hit=null;double fraction=0;foreach(Pipe pipe in project.Pipes){Node a=project.Nodes.First(v=>v.Id==pipe.A),b=project.Nodes.First(v=>v.Id==pipe.B);double dx=b.X-a.X,dy=b.Y-a.Y;double f=((e.X-a.X)*dx+(e.Y-a.Y)*dy)/Math.Max(1,dx*dx+dy*dy);f=Math.Max(0,Math.Min(1,f));if(Math.Pow(e.X-a.X-f*dx,2)+Math.Pow(e.Y-a.Y-f*dy,2)<120){hit=pipe;fraction=f;break;}}if(hit!=null){selected=hit;if(mode=="漏点"){hit.Leak=true;hit.Position=Math.Round(Math.Max(5,Math.Min(95,fraction*100)),1);Changed();}props.SelectedObject=hit;}}
  canvas.Invalidate();
 }
 void MouseMoveNet(object sender,MouseEventArgs e){if(drag!=null&&e.Button==MouseButtons.Left){drag.X=Math.Max(25,Math.Min(canvas.Width-35,e.X));drag.Y=Math.Max(25,Math.Min(canvas.Height-35,e.Y));dirty=true;canvas.Invalidate();}}
 void PaintNetwork(object sender,PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(Pen grid=new Pen(Color.FromArgb(236,241,246)))for(int x=0;x<canvas.Width;x+=25)for(int y=0;y<canvas.Height;y+=25)g.DrawEllipse(grid,x,y,1,1);
  foreach(Pipe pipe in project.Pipes){Node a=project.Nodes.FirstOrDefault(n=>n.Id==pipe.A),b=project.Nodes.FirstOrDefault(n=>n.Id==pipe.B);if(a==null||b==null)continue;using(Pen pen=new Pen(selected==pipe?Color.FromArgb(241,173,61):blue,selected==pipe?6:4)){pen.CustomEndCap=new AdjustableArrowCap(3,4);g.DrawLine(pen,a.X,a.Y,b.X,b.Y);}float mx=(a.X+b.X)/2,my=(a.Y+b.Y)/2;string label=pipe.Name+" · "+pipe.Length.ToString("0.#")+" m";SizeF size=g.MeasureString(label,Font);g.FillRectangle(Brushes.White,mx-size.Width/2,my-26,size.Width+6,20);g.DrawString(label,Font,Brushes.DimGray,mx-size.Width/2+2,my-25);
   if(pipe.Leak){float x=(float)(a.X+(b.X-a.X)*pipe.Position/100),y=(float)(a.Y+(b.Y-a.Y)*pipe.Position/100);PointF[] diamond={new PointF(x,y-10),new PointF(x+10,y),new PointF(x,y+10),new PointF(x-10,y)};g.FillPolygon(Brushes.OrangeRed,diamond);g.DrawString("漏点 Ø"+pipe.Hole.ToString("0.#")+" mm",Font,Brushes.OrangeRed,x+12,y+5);}}
  foreach(Node n in project.Nodes){Color c=n.Source?blue:n.Sensor?teal:Color.SlateGray;using(SolidBrush brush=new SolidBrush(c)){g.FillEllipse(brush,n.X-12,n.Y-12,24,24);}g.DrawEllipse(selected==n||linkStart==n?Pens.Orange:Pens.White,n.X-15,n.Y-15,30,30);g.DrawString(n.Source?"S":n.Sensor?"P":"•",Font,Brushes.White,n.X-6,n.Y-9);g.DrawString(n.Name,Font,Brushes.DimGray,n.X-20,n.Y+20);}
  g.DrawString("S 恒压源    P 压力与振动测点    ◆ 漏点    画布仅表示连接关系",Font,Brushes.SlateGray,20,20);
 }
 async Task Run(){if(busy)return;props.Focus();string error=Project.Validate(project);if(error!=null){MessageBox.Show(this,error,"参数检查");return;}busy=true;toolbar.Enabled=false;props.Enabled=false;canvas.Enabled=false;status.Text="正在计算压力与振动响应…";Project snapshot=Json.Read<Project>(Json.Write(project));try{result=await Task.Run(()=>Engine.Run(snapshot));Plot();status.Text="仿真完成  |  "+result.Names.Count+" 个测点  |  "+result.Time.Length+" 个采样点  |  时长 "+snapshot.Settings.Duration+" s";}catch(Exception ex){result=null;MessageBox.Show(this,ex.Message,"计算提示");status.Text="计算未完成，请检查参数";}finally{busy=false;toolbar.Enabled=true;props.Enabled=true;canvas.Enabled=true;}}
 void Plot(){chart.Series.Clear();Color[] colors={teal,Color.FromArgb(213,100,53),blue,Color.MediumPurple,Color.ForestGreen};for(int j=0;j<result.Values.Count;j++){for(int c=0;c<2;c++){Series line=new Series(result.Names[j]+" "+(c==0?"压力":"振动")+" ["+(j+1)+"]"){ChartArea=c==0?"压力":"振动",ChartType=SeriesChartType.FastLine,BorderWidth=2,Color=colors[j%colors.Length],IsVisibleInLegend=c==0,LegendText=result.Names[j]};double[] vals=c==0?result.Values[j]:result.Vibration[j];for(int i=0;i<result.Time.Length;i++)line.Points.AddXY(result.Time[i],vals[i]);chart.Series.Add(line);}}foreach(ChartArea area in chart.ChartAreas)area.RecalculateAxesScale();sensor.Items.Clear();foreach(string n in result.Names)sensor.Items.Add(n);if(sensor.Items.Count>0)sensor.SelectedIndex=0;tabs.SelectedIndex=1;}
 void UpdateAi(){if(result==null||sensor.SelectedIndex<0)return;for(int j=0;j<result.Values.Count;j++)for(int c=0;c<2;c++)chart.Series[j*2+c].BorderWidth=j==sensor.SelectedIndex?3:1;ai.Text=model==null?"AI 模型未加载。":model.Predict(result);}
 void ExportCsv(){if(result==null)throw new Exception("请先运行仿真。");using(SaveFileDialog d=new SaveFileDialog{Filter="CSV 数据 (*.csv)|*.csv",FileName="压力振动波形_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".csv",InitialDirectory=Folder("04_输出结果")})if(d.ShowDialog()==DialogResult.OK){Engine.Csv(result,d.FileName);File.WriteAllText(Path.ChangeExtension(d.FileName,".工程.json"),Json.Write(project),Encoding.UTF8);status.Text="已导出波形数据及对应工程参数";}}
 void ExportPng(){if(result==null)throw new Exception("请先运行仿真。");using(SaveFileDialog d=new SaveFileDialog{Filter="PNG 图片 (*.png)|*.png",FileName="压力振动波形.png",InitialDirectory=Folder("04_输出结果")})if(d.ShowDialog()==DialogResult.OK){chart.SaveImage(d.FileName,ChartImageFormat.Png);status.Text="波形图片已导出";}}
 void Help(){MessageBox.Show(this,"快速操作\r\n\r\n1. 选择支路或环网示例，或新建后添加节点。\r\n2. 连接管段：依次单击两个节点。\r\n3. 选择管段，在右侧填写实际长度、内径、波速。\r\n4. 设置漏点：单击管段，在属性中填写孔径和位置。\r\n5. 计算设置：填写初始表压、时长、开始时间。\r\n6. 运行仿真，查看压力、振动曲线和是否泄漏的判断。\r\n7. 导出 CSV 或波形图，保存工程便于复现。\r\n\r\n节点可拖动；移动不改变物理长度。\r\n每个连通网络须有恒压源和非源压力测点。\r\n端点默认为封闭端；漏点释放到零表压环境。\r\n漏点位置是仿真输入，AI 不输出定位结果。","澜析使用说明");}
 public async Task VerifyUi(string folder){
  List<string> log=new List<string>();dirty=false;New();
  MouseDownNet(canvas,new MouseEventArgs(MouseButtons.Left,1,100,180,0));MouseDownNet(canvas,new MouseEventArgs(MouseButtons.Left,1,300,180,0));MouseDownNet(canvas,new MouseEventArgs(MouseButtons.Left,1,520,180,0));
  if(project.Nodes.Count!=3||!project.Nodes[0].Source)throw new Exception("新建节点交互失败");log.Add("通过  添加节点与自动设置恒压源");
  Mode("连接");MouseDownNet(canvas,new MouseEventArgs(MouseButtons.Left,1,100,180,0));MouseDownNet(canvas,new MouseEventArgs(MouseButtons.Left,1,300,180,0));MouseDownNet(canvas,new MouseEventArgs(MouseButtons.Left,1,300,180,0));MouseDownNet(canvas,new MouseEventArgs(MouseButtons.Left,1,520,180,0));
  if(project.Pipes.Count!=2)throw new Exception("连接管段交互失败");log.Add("通过  两次节点点击建立管段");
  Mode("漏点");MouseDownNet(canvas,new MouseEventArgs(MouseButtons.Left,1,410,180,0));if(!project.Pipes[1].Leak||Math.Abs(project.Pipes[1].Position-50)>.1)throw new Exception("漏点设置交互失败");log.Add("通过  管段点击添加漏点及位置映射");
  await Run();if(result==null||chart.Series.Count!=4||sensor.Items.Count!=2||sensor.SelectedIndex!=0)throw new Exception("仿真结果界面绑定失败");log.Add("通过  异步计算与曲线及测点控件绑定");
  sensor.SelectedIndex=1;if(!ai.Text.Contains("参考票数")||!ai.Text.Contains("本功能不输出漏点位置"))throw new Exception("AI测点切换失败");log.Add("通过  测点切换更新AI判读");
  Changed();if(result!=null||chart.Series.Count!=0||sensor.Items.Count!=0)throw new Exception("旧结果未清除");log.Add("通过  工程修改使旧结果失效");
  selected=project.Nodes[1];Delete();if(project.Nodes.Count!=2||project.Pipes.Count!=0)throw new Exception("删除节点关联清理失败");log.Add("通过  删除节点同步删除连接管段");dirty=false;File.WriteAllLines(Path.Combine(folder,"界面交互验证.txt"),log,Encoding.UTF8);
 }
 public void CaptureUi(string folder){Directory.CreateDirectory(folder);result=Engine.Run(project);Plot();status.Text="仿真完成  |  3 个测点  |  2001 个采样点  |  时长 4 s";tabs.SelectedIndex=0;Refresh();Application.DoEvents();using(Bitmap b=new Bitmap(Width,Height)){DrawToBitmap(b,new Rectangle(0,0,Width,Height));b.Save(Path.Combine(folder,"管网工作台.png"));}tabs.SelectedIndex=1;Refresh();Application.DoEvents();chart.SaveImage(Path.Combine(folder,"压力振动波形.png"),ChartImageFormat.Png);using(Bitmap b=new Bitmap(Width,Height)){DrawToBitmap(b,new Rectangle(0,0,Width,Height));b.Save(Path.Combine(folder,"双通道检测界面.png"));}Engine.Csv(result,Path.Combine(folder,"示例压力振动.csv"));}
}
public static class Program {
 [STAThread] public static int Main(string[] args){
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  try{if(args.Length>0&&args[0]=="--train"){Model.Train(args[1]);return 0;}
   if(args.Length>0&&args[0]=="--test"){Test(args[1]);return 0;}
   MainForm f=new MainForm();if(args.Length>0&&args[0]=="--ui-test"){f.Shown+=async(s,e)=>{try{await f.VerifyUi(args[1]);}catch(Exception ex){File.WriteAllText(Path.Combine(args[1],"界面交互错误.txt"),ex.ToString());}finally{f.Close();}};}if(args.Length>0&&args[0]=="--capture"){f.Shown+=(s,e)=>{try{f.CaptureUi(args[1]);}finally{f.Close();}};}Application.Run(f);return 0;
  }catch(Exception ex){if(args.Length>0){File.WriteAllText(Path.Combine(args.Length>1?args[1]:AppDomain.CurrentDomain.BaseDirectory,"运行错误.txt"),ex.ToString());return 1;}MessageBox.Show(ex.Message,"澜析运行提示");return 1;}
 }
 static void Assert(bool condition,string text,List<string> log){if(!condition)throw new Exception("验证失败："+text);log.Add("通过  "+text);}
 static void Test(string folder){
  Directory.CreateDirectory(folder);List<string> log=new List<string>();
  for(int k=0;k<3;k++){Project p=Project.Demo(k);p.Settings.Noise=0;p.Settings.VibrationNoise=0;Result r=Engine.Run(p);Assert(r.Time.Length==2001&&r.Values.Count==3,"模板 "+k+" 采样数及通道数",log);Assert(r.Values.All(y=>y.All(v=>!double.IsNaN(v)&&v>0)),"模板 "+k+" 压力有限且为正",log);Assert(r.Values.All(y=>y.Take(450).All(v=>Math.Abs(v-.6)<1e-10)),"事件发生前维持平衡压力",log);Assert(r.Values.Any(y=>y.Min()<.599),"漏点开启引起压力下降",log);Assert(r.Vibration.Count==r.Values.Count&&r.Vibration.All(y=>y.Length==r.Time.Length&&y.All(v=>!double.IsNaN(v)&&!double.IsInfinity(v))),"双通道同步与振动有限值",log);Assert(r.Vibration.All(y=>y.Take(450).All(v=>Math.Abs(v)<1e-8)),"事件前振动基线为零",log);Assert(r.Vibration.Any(y=>y.Max(v=>Math.Abs(v))>.001),"压力扰动激发振动响应",log);Result repeat=Engine.Run(p);Assert(r.Values[0].SequenceEqual(repeat.Values[0]),"同一工程计算可复现",log);File.WriteAllText(Path.Combine(folder,"模板"+k+".json"),Json.Write(p),Encoding.UTF8);foreach(Pipe e in p.Pipes)e.Leak=false;r=Engine.Run(p);Assert(r.Values.All(y=>y.All(v=>Math.Abs(v-.6)<1e-9)),"无泄漏恒压平衡",log);Assert(r.Vibration.All(y=>y.All(v=>Math.Abs(v)<1e-8)),"无泄漏零振动基线",log);}
  Project a=Project.Demo(1);a.Settings.Noise=0;Result coarse=Engine.Run(a,8),fine=Engine.Run(a,4),finer=Engine.Run(a,2);Func<Result,Result,double> rmse=(r1,r2)=>Math.Sqrt(r1.Values[0].Zip(r2.Values[0],(x,y)=>(x-y)*(x-y)).Average());double ec=rmse(coarse,fine),ef=rmse(fine,finer);Assert(ef<ec,"网格加密差值下降："+ec.ToString("G5")+" → "+ef.ToString("G5")+" MPa",log);
  Project disconnected=Project.Demo(1);disconnected.Nodes.Add(new Node{Id=99,Name="孤立",X=20,Y=20,Sensor=true});Assert(Project.Validate(disconnected)!=null,"拒绝孤立节点",log);
  Project invalid=Project.Demo(1);invalid.Pipes[0].Length=double.NaN;Assert(Project.Validate(invalid)!=null,"拒绝非有限参数",log);
  Project round=Json.Read<Project>(Json.Write(a));Assert(Json.Write(a)==Json.Write(round),"工程保存读取保持参数",log);
  a.Pipes[2].Leak=true;Assert(Engine.Run(a).Values.All(y=>y.All(v=>v>0)),"多管段漏点计算",log);
  Model model=Model.Load();Assert(model!=null&&model.Samples.Count==240,"AI 模型内嵌加载及样本数",log);Assert(model.Vote(Model.Features(fine))>=4,"演示泄漏波形的 AI 判读",log);
  a=Project.Demo(1);foreach(Pipe e in a.Pipes)e.Leak=false;a.Settings.Noise=.0002;Assert(model.Vote(Model.Features(Engine.Run(a)))<4,"演示正常波形的 AI 判读",log);
  Engine.Csv(fine,Path.Combine(folder,"导出校验.csv"));Assert(File.ReadAllLines(Path.Combine(folder,"导出校验.csv")).Length==2002,"CSV 表头与样本行数",log);
  Assert(File.ReadAllLines(Path.Combine(folder,"导出校验.csv"))[0].Split(',').Length==7,"CSV 同时包含三测点压力与振动",log);
  Project bad=Project.Demo(1);bad.Settings.SampleRate=50;Assert(Project.Validate(bad)!=null,"拒绝不满足振动采样要求的设置",log);
  Oscillator osc=new Oscillator(20,.1);double stepdt=.0001,maxerr=0,w=2*Math.PI*20,wd=w*Math.Sqrt(.99);for(int i=1;i<=10000;i++){double t=i*stepdt;double computed=osc.Step(1,stepdt);double exact=Math.Exp(-.1*w*t)*(Math.Cos(wd*t)-.1/Math.Sqrt(.99)*Math.Sin(wd*t));if(i>1)maxerr=Math.Max(maxerr,Math.Abs(computed-exact));}Assert(maxerr<.02,"振动积分对照单位阶跃解析加速度，最大误差="+maxerr.ToString("G4"),log);
  Project g1=Project.Demo(1);g1.Settings.Noise=0;g1.Settings.VibrationNoise=0;Result vg1=Engine.Run(g1);g1.Settings.VibrationGain*=2;Result vg2=Engine.Run(g1);Assert(vg1.Values[0].SequenceEqual(vg2.Values[0]),"改变振动参数不改写压力结果",log);Assert(vg1.Vibration[0].Zip(vg2.Vibration[0],(x,y)=>Math.Abs(y-2*x)).Max()<1e-9,"振动响应系数线性检验",log);
  double[] realfeatures=Model.Features(vg1);double[] zeroV=vg1.Vibration[0];vg1.Vibration=vg1.Vibration.Select(y=>new double[y.Length]).ToList();Assert(!realfeatures.SequenceEqual(Model.Features(vg1)),"分类特征实际读取振动通道",log);
  string conclusion=model.Predict(vg2);Assert(conclusion.Contains("检测结果：")&&!conclusion.Contains("候选管段")&&!conclusion.Contains("预测位置"),"仅输出泄漏二分类",log);
  File.WriteAllLines(Path.Combine(folder,"功能验证.txt"),log,Encoding.UTF8);
 }
}
}



