module.exports = {
  module: {
    rules: [{ test: /\.css$/, include: /[\\/]monaco-editor[\\/]/, use: ['style-loader', 'css-loader'] }],
  },
};
