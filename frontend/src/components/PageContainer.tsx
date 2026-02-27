import type { ReactNode } from 'react';

interface Props {
  title: string;
  children: ReactNode;
}

export default function PageContainer({ title, children }: Props) {
  return (
    <div className="page-container">
      <h1>{title}</h1>
      {children}
    </div>
  );
}
