import React, {Component} from 'react';
import { Col, Grid, Row, Thumbnail, Table, ButtonGroup, Button } from 'react-bootstrap';

export class NetSettings extends Component {

    constructor(props) {
        super(props);

        this.state = {
            layers: []
        };
    }

    componentDidMount = async () => {
        await fetch('api/net/settings')
            .then(response => response.json())
            .then(data => this.setState({ layers: data }));
    };

    updateLayerType = (i, event) => {
        let layers = this.state.layers;
        layers[i].type = parseInt(event.target.value);

        if (layers[i].type == 2) {
            layers[i].activation = 0;
        }

        this.setState({ layers: layers });
    };

    updateNeuronsCount = (i, event) => {
        let layers = this.state.layers;
        layers[i].neuronsCount = parseInt(event.target.value);
        this.setState({ layers: layers });
    };

    updateKernelSize = (i, event) => {
        let layers = this.state.layers;
        layers[i].kernelSize = parseInt(event.target.value);
        this.setState({ layers: layers });
    };

    prepareTeachBatchFile = async () => {
        await fetch('api/net/teach/batch', {
            method: 'POST'
        });
    };

    render() {
        const layers = this.state.layers;
        let i = -1;

        return (
            <div>
                <Table striped bordered condensed hover>
                    <thead>
                        <tr>
                            <th>Type</th>
                            <th>Activation</th>
                            <th>Neurons</th>
                            <th>Kernel</th>
                        </tr>
                    </thead>
                    <tbody>
                        {layers.map(layer => 
                            <tr key={i++}>
                                <td>
                                    <select defaultValue={layer.type} onChange={this.updateLayerType.bind(this, i)}>
                                        <option value="0">Convolution</option>
                                        <option value="1">MLP</option>
                                        <option value="2">MaxPooling</option>
                                    </select>
                                </td>
                                <td>
                                    <select defaultValue={layer.activation} disabled={layer.type == 2}>
                                        <option value="0">None</option>
                                        <option value="1">BipolarSigmoid</option>
                                        <option value="2">Sigmoid</option>
                                        <option value="3">ELU</option>
                                        <option value="4">LeakyReLu</option>
                                        <option value="5">ReLu</option>
                                        <option value="6">LeCunTanh</option>
                                        <option value="7">AbsoluteReLU</option>
                                    </select>
                                </td>
                                <td>
                                    <input
                                        type="number"
                                        value={layer.neuronsCount || ''}
                                        onChange={this.updateNeuronsCount.bind(this, i)}
                                    />
                                </td>
                                <td>
                                    <input
                                        type="number"
                                        value={layer.kernelSize || ''}
                                        onChange={this.updateKernelSize.bind(this, i)}
                                    />
                                </td>
                            </tr>
                        )}
                    </tbody>
                </Table>
                <Row>
                    <Col sm={12}>
                        <ButtonGroup>
                            <Button>Apply</Button>
                            <Button onClick={this.prepareTeachBatchFile}>Prepare batch</Button>
                        </ButtonGroup>
                    </Col>
                </Row>
            </div>
        );
    }
}
