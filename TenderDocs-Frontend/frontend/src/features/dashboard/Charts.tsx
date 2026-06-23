import {
  Bar, BarChart, Cell, Pie, PieChart, ResponsiveContainer, XAxis, YAxis, Tooltip,
} from 'recharts';

const TEAL = ['#0F766E', '#1B8470', '#3E9A85', '#6FBBA8', '#A6D5C8', '#114E4A', '#15807A'];

export function DocumentsByTypeChart({ data }: { data: { type: string; count: number }[] }) {
  const max = Math.max(1, ...data.map((d) => d.count));
  return (
    <ResponsiveContainer width="100%" height={260}>
      <BarChart data={data} margin={{ top: 10, right: 8, left: -20, bottom: 28 }} barCategoryGap="28%">
        <YAxis allowDecimals={false} domain={[0, Math.ceil(max)]} tickLine={false} axisLine={false}
          tick={{ fill: '#94A3B8', fontSize: 12 }} width={36} />
        <XAxis dataKey="type" tickLine={false} axisLine={false} interval={0} height={50}
          tick={{ fill: '#94A3B8', fontSize: 11 }} angle={-12} textAnchor="end" />
        <Tooltip cursor={{ fill: 'rgba(15,118,110,0.06)' }}
          contentStyle={{ borderRadius: 12, border: '1px solid #ECEEF1', boxShadow: '0 4px 16px rgba(16,24,40,0.06)', fontSize: 12 }} />
        <Bar dataKey="count" fill="#117C6F" radius={[6, 6, 0, 0]} maxBarSize={520} />
      </BarChart>
    </ResponsiveContainer>
  );
}

export function TypeDistributionChart({ data }: { data: { type: string; count: number }[] }) {
  return (
    <ResponsiveContainer width="100%" height={260}>
      <PieChart>
        <Pie data={data} dataKey="count" nameKey="type" innerRadius={62} outerRadius={96} paddingAngle={2} stroke="none">
          {data.map((_, i) => <Cell key={i} fill={TEAL[i % TEAL.length]} />)}
        </Pie>
        <Tooltip contentStyle={{ borderRadius: 12, border: '1px solid #ECEEF1', fontSize: 12 }} />
      </PieChart>
    </ResponsiveContainer>
  );
}
