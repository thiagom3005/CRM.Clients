interface Props {
  message: string;
}

export default function ErrorMessage({ message }: Props) {
  return <p className="error-message">{message}</p>;
}
